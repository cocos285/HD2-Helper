namespace HD2_Helper;

public partial class MainForm
{
    private static bool _autoLoadoutEnabled = true;
    private static string _saveFilePath = "";
    private readonly SavedLoadoutState _savedState = new();
    private Dictionary<uint, string>? _saveCodes;
    private Dictionary<uint, string> _baseCodes = new();
    private Dictionary<uint, string> _userCodes = new();
    private static string UserCodesPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HD2-Helper", "user_stratagem_codes.json");
    private void EnsureCodeMaps()
    {
        if (_saveCodes != null) return;
        var baseCodes = SavedLoadoutReader.LoadCodes(Path.Combine(AppContext.BaseDirectory, "stratagem_codes.json"), _sequenceMap.Keys);
        var userCodes = UserCodeStore.Load(UserCodesPath, _sequenceMap.Keys);
        _baseCodes = baseCodes;
        _userCodes = userCodes.Where(p => !baseCodes.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value);
        RebuildCodeMap();
    }
    private void RebuildCodeMap()
    {
        _saveCodes = new Dictionary<uint, string>(_userCodes);
        foreach (var pair in _baseCodes) _saveCodes[pair.Key] = pair.Value;
    }
    private void UpdateUserCode(string codeText, string? name, bool delete)
    {
        try
        {
            EnsureCodeMaps();
            if (codeText.Length != 8 || !uint.TryParse(codeText, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var code))
                throw new InvalidDataException("코드는 8자리 16진수여야 합니다.");
            if (_baseCodes.ContainsKey(code) && !delete) throw new InvalidDataException("기본 코드표에 등록된 코드는 변경할 수 없습니다.");
            if (!delete && (name == null || !_sequenceMap.TryGetValue(name, out var sequence) || sequence.Length == 0))
                throw new InvalidDataException("목록에서 스트라타잼을 선택해 주세요.");
            var updated = new Dictionary<uint, string>(_userCodes);
            if (delete) updated.Remove(code); else updated[code] = name!;
            UserCodeStore.Save(UserCodesPath, updated);
            _userCodes = updated;
            RebuildCodeMap();
            // Reconfirm the file so a loadout changed during registration never uses stale slots.
            ResetFileLoadout();
            _loadoutStatus = delete ? "등록 삭제됨 · 저장 파일 재확인 중" : "등록 저장됨 · 저장 파일 재확인 중";
            SendSettingsToWeb();
            _webView?.CoreWebView2?.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(new { type = "USER_CODE_RESULT", ok = true }));
        }
        catch (Exception ex)
        {
            _webView?.CoreWebView2?.PostWebMessageAsJson(System.Text.Json.JsonSerializer.Serialize(new { type = "USER_CODE_RESULT", ok = false, message = ex.Message }));
        }
    }
    private string?[] _detectedLoadout = new string?[4];
    private string[] _detectedDescriptions = Array.Empty<string>();
    private System.Windows.Forms.Timer? _loadoutTimer;
    private bool _loadoutBusy;
    private int _loadoutGeneration;
    private string _loadoutStatus = "저장 파일 확인 대기";
    private const int SavePollInterval = 1000;

    private string?[] EffectiveSlots
    {
        get
        {
            if (!_autoLoadoutEnabled) return _currentSlots;
            var slots = new string?[10];
            Array.Copy(_detectedLoadout, slots, 4);
            return slots;
        }
    }

    private void ResetFileLoadout()
    {
        _loadoutGeneration++;
        _savedState.Reset();
        _detectedLoadout = new string?[4];
        _detectedDescriptions = Array.Empty<string>();
        _loadoutStatus = "저장 파일 확인 대기";
    }

    private void ChooseSaveFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "게임의 testament_new.sav 선택",
            Filter = "헬다이버즈 저장 파일 (testament_new.sav)|testament_new.sav",
            CheckFileExists = true,
            FileName = "testament_new.sav"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _saveFilePath = dialog.FileName;
        ResetFileLoadout();
        SaveSetting();
        SendSettingsToWeb();
    }

    private static string[] DiscoverSaveFiles()
    {
        var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam")
        };
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (key?.GetValue("SteamPath") is string path && !string.IsNullOrWhiteSpace(path)) steamRoots.Add(path);
        }
        catch (System.Security.SecurityException) { }
        catch (UnauthorizedAccessException) { }
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string steam in steamRoots)
        {
            try
            {
                string users = Path.Combine(steam, "userdata");
                if (!Directory.Exists(users)) continue;
                foreach (string user in Directory.EnumerateDirectories(users))
                {
                    string file = Path.Combine(user, "553850", "remote", "testament_new.sav");
                    if (File.Exists(file)) files.Add(Path.GetFullPath(file));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return files.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void StartLoadoutMonitor()
    {
        try { EnsureCodeMaps(); } catch (Exception ex) { _loadoutStatus = ex.Message; }
        if (string.IsNullOrWhiteSpace(_saveFilePath))
        {
            var candidates = DiscoverSaveFiles();
            if (candidates.Length == 1) _saveFilePath = candidates[0];
            else _loadoutStatus = candidates.Length == 0 ? "저장 파일을 선택해 주세요" : "여러 계정 발견 · 사용할 저장 파일을 선택해 주세요";
        }
        _loadoutTimer = new System.Windows.Forms.Timer { Interval = SavePollInterval };
        _loadoutTimer.Tick += async (_, _) =>
        {
            if (_loadoutBusy || !_autoLoadoutEnabled || string.IsNullOrWhiteSpace(_saveFilePath)) return;
            _loadoutBusy = true;
            int generation = _loadoutGeneration;
            string path = _saveFilePath;
            try
            {
                EnsureCodeMaps();
                var snapshot = await Task.Run(() => SavedLoadoutReader.ReadSnapshot(path));
                if (IsDisposed || !_autoLoadoutEnabled || generation != _loadoutGeneration) return;
                if (!_savedState.Poll(snapshot))
                {
                    _detectedLoadout = new string?[4];
                    _detectedDescriptions = Array.Empty<string>();
                    _loadoutStatus = "변경 감지 · 저장 완료 확인 중";
                }
                else
                {
                    var codes = _savedState.Codes!;
                    _detectedLoadout = SavedLoadoutReader.Resolve(codes, _saveCodes!);
                    _detectedDescriptions = codes.Select((code, i) => _detectedLoadout[i] ?? $"미등록 코드 {code:X8} · 사용 안 함").ToArray();
                    int known = _detectedLoadout.Count(n => n != null);
                    _loadoutStatus = known == 4 ? "파일 연결 완료 · 4/4" : $"파일 연결 {known}/4 · 미등록 슬롯 제외";
                }
                SendSettingsToWeb();
            }
            catch (Exception ex)
            {
                if (!IsDisposed && generation == _loadoutGeneration)
                {
                    _savedState.Reset();
                    _detectedLoadout = new string?[4];
                    _detectedDescriptions = Array.Empty<string>();
                    _loadoutStatus = "파일 읽기 대기 · " + ex.Message;
                    SendSettingsToWeb();
                }
            }
            finally { _loadoutBusy = false; }
        };
        _loadoutTimer.Start();
    }
}


