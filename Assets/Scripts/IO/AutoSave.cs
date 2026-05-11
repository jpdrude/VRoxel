using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class AutoSave : MonoBehaviour
{
    [SerializeField]
    float interval = 120;

    float timer = 0;


    // Update is called once per frame
    void Update()
    {
        if (timer > interval)
        {
            Save();
            timer = 0;
            return;
        }

        timer += Time.deltaTime;
    }

    void Save()
    {
        if (BlockContainer.Container.GetCount() < 10)
            return;

        string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VRoxel";

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string[] files = null;

        try
        {
            files = Directory.GetFiles(dir);
        }
        catch (Exception e)
        {
            Debug.LogError("An error occurred in the AutoSave System: " + e.Message);
        }

        string[] slotNames = { "autosave01.vrxl", "autosave02.vrxl", "autosave03.vrxl", "autosave04.vrxl", "autosave05.vrxl" };
        string[] slotPaths = new string[5];
        DateTime[] slotTimes = new DateTime[5];

        if (files != null)
        {
            foreach (string file in files)
            {
                int idx = Array.IndexOf(slotNames, Path.GetFileName(file));
                if (idx < 0) continue;
                slotPaths[idx] = file;
                slotTimes[idx] = new FileInfo(file).LastWriteTime;
            }
        }

        string savePath = null;

        for (int i = 0; i < slotPaths.Length; ++i)
        {
            if (slotPaths[i] == null)
            {
                savePath = dir + "\\" + slotNames[i];
                break;
            }
        }

        if (savePath == null)
        {
            int oldestIdx = 0;
            for (int i = 1; i < slotTimes.Length; ++i)
            {
                if (slotTimes[i] < slotTimes[oldestIdx])
                    oldestIdx = i;
            }
            savePath = slotPaths[oldestIdx];
        }

        AssemblyIO.SaveAssemblyLayout(savePath, true);
    }
}
