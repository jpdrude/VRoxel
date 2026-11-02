using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

// whisper.cpp's server (whisper-server.exe), started hidden on first use: it loads the model once and transcribes the
// recordings SpeechToText posts to it on this machine. Stopped with VRoxel; one left over from an earlier session
// (e.g. after the editor crashed) is stopped before a new one starts, as it would hold the port and the GPU memory.
public class WhisperServer
{
    readonly SpeechToText.Settings settings;
    Process process;
    float startedAt = -100;

    // Loading the model takes a few seconds; requests are retried meanwhile
    const float StartTimeout = 60;

    public WhisperServer(SpeechToText.Settings settings)
    {
        this.settings = settings;
    }

    public string InferenceUrl { get { return "http://127.0.0.1:" + settings.port + "/inference"; } }

    public bool Starting { get { return process != null && !process.HasExited && Time.realtimeSinceStartup - startedAt < StartTimeout; } }

    // Starts the server if VRoxel hasn't yet; false (with the reason) if it can't
    public bool Ensure(out string error)
    {
        error = null;
        if (process != null && !process.HasExited)
            return true;

        string server = SpeechToText.ResolvePath(settings.serverPath);
        // The installer unpacks whisper.cpp's zip as it is, with the server in a Release folder
        string unpacked = Path.Combine(Path.GetDirectoryName(server), "Release", Path.GetFileName(server));
        if (!File.Exists(server) && File.Exists(unpacked))
            server = unpacked;
        string model = SpeechToText.ResolvePath(settings.modelPath);
        if (!File.Exists(server))
        {
            error = "Whisper server not found: " + server;
            return false;
        }
        if (!File.Exists(model))
        {
            error = "Whisper model not found: " + model;
            return false;
        }

        StopLeftovers(server);

        string arguments = "-m \"" + model + "\" --host 127.0.0.1 --port " + settings.port + " -l " + settings.language +
            " -t " + Mathf.Max(1, settings.threads) + " " + settings.serverArguments;
        try
        {
            process = Process.Start(new ProcessStartInfo(server, arguments.Trim())
            {
                WorkingDirectory = Path.GetDirectoryName(server),
                UseShellExecute = false,
                CreateNoWindow = true
            });
            startedAt = Time.realtimeSinceStartup;
            return true;
        }
        catch (Exception e)
        {
            error = "Whisper server could not be started: " + e.Message;
            process = null;
            return false;
        }
    }

    // Servers started from the same file, which only VRoxel starts
    static void StopLeftovers(string server)
    {
        foreach (Process other in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(server)))
        {
            try
            {
                if (string.Equals(Path.GetFullPath(other.MainModule.FileName), server, StringComparison.OrdinalIgnoreCase))
                    other.Kill();
            }
            catch (Exception)
            {
                // Exited meanwhile, or not ours to inspect
            }
            finally
            {
                other.Dispose();
            }
        }
    }

    public void Stop()
    {
        try
        {
            if (process != null && !process.HasExited)
                process.Kill();
        }
        catch (Exception)
        {
            // Exited in the meantime
        }
        process = null;
    }
}
