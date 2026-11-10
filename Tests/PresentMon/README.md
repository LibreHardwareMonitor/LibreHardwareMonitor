# PresentMon native API regression tests

Requires Windows x64, .NET 10 SDK, and the Visual Studio C++ tools. No PresentMon
installation, administrator rights, or game is required. The stub implements the
public C ABI, deliberately returns non-contiguous field offsets, and tracks native
resource ownership. These tests do not replace a live test with the official SDK.

From an **x64 Native Tools Command Prompt**, at the repository root:

```bat
cl /nologo /LD /EHsc Tests\PresentMon\NativeStub.cpp /Fe:Tests\PresentMon\NativeStub.dll /Fo:Tests\PresentMon\NativeStub.obj
dotnet run --project Tests\PresentMon\PresentMonRegression.csproj -c Release -p:Platform=x64 -- Tests\PresentMon\NativeStub.dll
```

To probe the official middleware and a running application's PID:

```bat
dotnet run --project Tests\PresentMon\PresentMonRegression.csproj -c Release -p:Platform=x64 -- --live "C:\Program Files\Intel\PresentMonSharedService\PresentMonAPI2.dll" 1234
```

The probe prints eight one-second samples and releases its session. It does not
install or stop the system service. An optional final control-pipe argument
specifies a private control pipe and selects the SDK's playback tracking API for
a separate ETL replay service. This can be used with PresentMon's own
`Tests/Gold/test_case_5.etl` (target PID 24892), `--pace-playback`, and
`--control-pipe \\.\pipe\lhm_pm_regression` to validate the real middleware
without a running game. The replay service also needs a distinct shared-memory
prefix, e.g. `--shm-name-prefix Local\lhm_pm_replay`.
