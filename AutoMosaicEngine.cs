using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ImageMosaicEditor;

internal sealed class AutoMosaicResult
{
    [JsonPropertyName("status")] public string Status { get; set; } = "error";
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("processed")] public int Processed { get; set; }
    [JsonPropertyName("undetected")] public int Undetected { get; set; }
    [JsonPropertyName("errors")] public int Errors { get; set; }
    [JsonPropertyName("warning")] public string? Warning { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

internal static class AutoMosaicEngine
{
    private sealed record PythonCommand(string FileName, string[] PrefixArguments);
    private static PythonCommand? _cachedPython;

    private static string ScriptPath => Path.Combine(AppContext.BaseDirectory, "python", "auto_mosaic_bridge.py");
    private static string BundledPythonPath => Path.Combine(AppContext.BaseDirectory, "python-runtime", "python.exe");

    public static async Task<AutoMosaicResult> ProcessFileAsync(
        string inputPath, string outputPath, AutoMosaicSettings settings,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string>
        {
            ScriptPath, "--input-file", inputPath, "--output-file", outputPath
        };
        AppendSettings(args, settings);
        return await RunBridgeAsync(args, cancellationToken);
    }

    public static async Task<AutoMosaicResult> ProcessFolderAsync(
        string inputDirectory, string outputDirectory, AutoMosaicSettings settings,
        CancellationToken cancellationToken = default)
    {
        var args = new List<string>
        {
            ScriptPath, "--input-dir", inputDirectory,
            "--output-dir", outputDirectory, "--copy-undetected"
        };
        AppendSettings(args, settings);
        return await RunBridgeAsync(args, cancellationToken);
    }

    private static void AppendSettings(List<string> args, AutoMosaicSettings settings)
    {
        args.AddRange([
            "--mode", settings.Mode,
            "--strength", settings.Strength.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--confidence", settings.Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--padding", settings.Padding.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "--detector", settings.Detector
        ]);
        if (settings.IncludeNipple) args.Add("--include-nipple");
        if (settings.IncludeAnus) args.Add("--include-anus");
        if (settings.IncludeTesticles) args.Add("--include-testicles");
        if (!string.IsNullOrWhiteSpace(settings.Ntd11ModelPath))
            args.AddRange(["--ntd11-model", settings.Ntd11ModelPath]);
    }

    private static async Task<AutoMosaicResult> RunBridgeAsync(
        List<string> bridgeArgs, CancellationToken cancellationToken)
    {
        if (!File.Exists(ScriptPath))
            throw new FileNotFoundException("자동 검출 Python 스크립트를 찾을 수 없습니다.", ScriptPath);

        PythonCommand python = await FindPythonAsync(cancellationToken);
        var args = new List<string>();
        args.AddRange(python.PrefixArguments);
        args.AddRange(bridgeArgs);

        var result = await RunProcessAsync(python.FileName, args, cancellationToken);
        string json = result.StdOut
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault(line => line.TrimStart().StartsWith('{')) ?? string.Empty;

        AutoMosaicResult? parsed = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { parsed = JsonSerializer.Deserialize<AutoMosaicResult>(json); }
            catch (JsonException) { }
        }

        if (result.ExitCode != 0)
        {
            string message = parsed?.Error ?? result.StdErr.Trim();
            if (string.IsNullOrWhiteSpace(message)) message = result.StdOut.Trim();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message)
                ? $"자동 모자이크 엔진이 종료 코드 {result.ExitCode}로 실패했습니다."
                : message);
        }

        return parsed ?? throw new InvalidOperationException(
            $"자동 모자이크 엔진의 응답을 해석할 수 없습니다.\n{result.StdErr}\n{result.StdOut}".Trim());
    }

    private static async Task<PythonCommand> FindPythonAsync(CancellationToken cancellationToken)
    {
        if (_cachedPython != null) return _cachedPython;

        if (File.Exists(BundledPythonPath))
        {
            var bundled = new PythonCommand(BundledPythonPath, []);
            var bundledResult = await RunProcessAsync(
                bundled.FileName, ["--version"], cancellationToken, throwOnStartFailure: false);
            if (bundledResult.ExitCode == 0)
            {
                _cachedPython = bundled;
                return bundled;
            }
        }

        PythonCommand[] candidates =
        [
            new("py", ["-3.12"]),
            new("py", ["-3.11"]),
            new("py", ["-3.10"]),
            new("py", ["-3.13"]),
            new("py", ["-3"]),
            new("python", []),
            new("python3", [])
        ];

        foreach (var candidate in candidates)
        {
            var args = new List<string>();
            args.AddRange(candidate.PrefixArguments);
            args.Add("--version");
            var result = await RunProcessAsync(
                candidate.FileName, args, cancellationToken, throwOnStartFailure: false);
            if (result.ExitCode == 0)
            {
                _cachedPython = candidate;
                return candidate;
            }
        }

        throw new InvalidOperationException(
            "번들 Python 런타임을 찾을 수 없습니다. Release ZIP을 다시 내려받거나 Python 3.10 이상을 설치하세요.");
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken,
        bool throwOnStartFailure = true)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        psi.Environment["PYTHONNOUSERSITE"] = "1";
        foreach (string arg in arguments) psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException($"프로세스를 시작할 수 없습니다: {fileName}");
        }
        catch when (!throwOnStartFailure)
        {
            return (-1, string.Empty, string.Empty);
        }

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}
