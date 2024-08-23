using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

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

        string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\VRoxel";


        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        string[] files = null;

        try
        {
            // Get all files in the directory
            files = Directory.GetFiles(path);

        }
        catch (Exception e)
        {
            Debug.LogError("An error occurred in the AutoSave System: " + e.Message);
        }

        if (files == null || files.Length == 0)
            path += "\\autosave01.vrxl";
        else
        {
            bool save01 = false;
            bool save02 = false;
            bool save03 = false;
            bool save04 = false;
            bool save05 = false;

            foreach (string file in files)
            {
                if (Path.GetFileName(file) == "autosave01.vrxl")
                    save01 = true;
                else if (Path.GetFileName(file) == "autosave02.vrxl")
                    save02 = true;
                else if (Path.GetFileName(file) == "autosave03.vrxl")
                    save03 = true;
                else if (Path.GetFileName(file) == "autosave04.vrxl")
                    save04 = true;
                else if (Path.GetFileName(file) == "autosave05.vrxl")
                    save05 = true;
            }

            if (!save01)
                path += "\\autosave01.vrxl";
            else if (!save02)
                path += "\\autosave02.vrxl";
            else if (!save03)
                path += "\\autosave03.vrxl";
            else if (!save04)
                path += "\\autosave04.vrxl";
            else if (!save05)
                path += "\\autosave05.vrxl";
            else
            {
                path = files[0];

                foreach (string file in files)
                {
                    if (!file.Contains("autosave"))
                        return;

                    FileInfo pathInfo = new FileInfo(path);
                    FileInfo current = new FileInfo(file);

                    if (current.LastWriteTime < pathInfo.LastWriteTime)
                        path = file;
                }

                Debug.Log("Saving oldest file. Oldest file is: " + Path.GetFileName(path));
            }
        }

        AssemblyIO.SaveAssemblyLayout(path, true);
    }
}
