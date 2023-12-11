using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using unitycodercom_PointCloudHelpers;
using System;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

public class PointCloud2Binary
{
    //File Formats
    string[] fileFormats = new string[] { "XYZ", "XYZRGB", "CGO", "ASC", "CATIA ASC", "PLY (ASCII)", "LAS", "PTS", "PCD (ASCII)" };
    string[] fileFormatInfo = new string[]{"sample: \"32.956900 5.632800 5.673400\"", // XYZ
			"sample: \"32.956900 5.632800 5.670000 128 190 232\"", // XYZRGB
			"sample: \"683,099976 880,200012 5544,700195\"", // CGO
			"sample: \" -1192.9 2643.6 5481.2\"", // ASC
			"sample: \"X 31022.1919 Y -3314.1098 Z 6152.5000\"", //  CATIA ASC
			"sample: \"-0.680891 -90.6809 0 204 204 204 255\"", // PLY (ASCII)
			"info: LAS 1.4", // LAS
			"info: 42.72464 -16.1426 -32.16625 10 88 23 98", // PTS
            "info: 42.72464 -16.1426 -32.16625 4.2108e+06" // PCD ASCII
		};


    public void Convert2Binary(string path, bool readRGB, string format)
    {
        string saveFilePath = path + ".bin";
        string fileToRead = path;

        int fileFormat = 0;
        bool readIntensity = false;
        long masterPointCount = 0;
        bool plyHasNormals = false;
        bool plyHasDensity = false;
        Vector3 autoOffset = Vector3.zero;
        bool flipYZ = true;

        if (!File.Exists(fileToRead)) return;

        long lines = 0;

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();

        switch (format)
        {
            case ("XYZ"):
                fileFormat = 0;
                readRGB = false;
                break;
            case ("XYZRGB"):
                fileFormat = 1;
                break;
            case ("PLY"):
                fileFormat = 5;
                break;
            case ("PTS"):
                fileFormat = 7;
                break;
            case ("ASC"):
                fileFormat = 3;
                break;
            case ("PCD"):
                fileFormat = 8;
                break;
        }

        using (StreamReader streamReader = new StreamReader(File.OpenRead(fileToRead)))
        {
            double x = 0, y = 0, z = 0;
            float r = 0, g = 0, b = 0; //,nx=0,ny=0,nz=0;
            string line = null;
            string[] row = null;

            PeekHeaderData headerCheck;
            headerCheck.x = 0; headerCheck.y = 0; headerCheck.z = 0;
            headerCheck.linesRead = 0;

            switch (fileFormats[fileFormat])
            {
                case "ASC": // ASC (space at front)
                    {
                        headerCheck = PeekHeader.PeekHeaderASC(streamReader, readRGB);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        lines = headerCheck.linesRead;
                    }
                    break;

                case "CGO": // CGO	(counter at first line and uses comma)
                    {
                        //                            headerCheck = PeekHeader.PeekHeaderCGO(txtReader, readRGB);
                        headerCheck = PeekHeader.PeekHeaderCGO(streamReader, ref readRGB, readIntensity, ref masterPointCount);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        lines = headerCheck.linesRead;
                    }
                    break;

                case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                    {
                        headerCheck = PeekHeader.PeekHeaderCATIA_ASC(streamReader, ref readRGB);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        lines = headerCheck.linesRead;
                    }
                    break;

                case "XYZRGB":
                case "XYZ": // XYZ RGB(INT)
                    {
                        headerCheck = PeekHeader.PeekHeaderXYZ(streamReader, ref readRGB);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        lines = headerCheck.linesRead;
                    }
                    break;

                case "PTS": // PTS (INT) (RGB)
                    {
                        headerCheck = PeekHeader.PeekHeaderPTS(streamReader, readRGB, readIntensity, ref masterPointCount);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        lines = headerCheck.linesRead;
                    }
                    break;

                case "PLY (ASCII)": // PLY (ASCII)
                    {
                        headerCheck = PeekHeader.PeekHeaderPLY(streamReader, readRGB, ref masterPointCount, ref plyHasNormals, ref plyHasDensity);
                        if (!headerCheck.readSuccess) { streamReader.Close(); return; }
                        readRGB = headerCheck.hasRGB;
                        //lines = headerCheck.linesRead;
                    }
                    break;

                case "PCD (ASCII)": // PCD (ASCII)
                    {
                        headerCheck = PeekHeader.PeekHeaderPCD(streamReader, ref readRGB, ref masterPointCount);
                        if (headerCheck.readSuccess == false) { streamReader.Close(); return; }
                    }
                    break;

                default:
                    Debug.LogError("PointCloud2Binary" + "> Unknown fileformat error (1) " + fileFormats[fileFormat]);
                    break;
            }

            autoOffset = -new Vector3((float)headerCheck.x, (float)headerCheck.y, (float)headerCheck.z);

            // get total amount of points from formats that have it
            if (fileFormats[fileFormat] == "PLY (ASCII)" || fileFormats[fileFormat] == "PTS" || fileFormats[fileFormat] == "CGO" || fileFormats[fileFormat] == "PCD (ASCII)")
            {
                lines = masterPointCount;

                // reset back to start of file
                streamReader.DiscardBufferedData();
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                streamReader.BaseStream.Position = 0;

                // get back to before first actual data line
                for (int i = 0; i < headerCheck.linesRead - 1; i++)
                {
                    streamReader.ReadLine();
                }
            }
            else
            { // other formats need to be read completely

                // reset back to start of file
                streamReader.DiscardBufferedData();
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                streamReader.BaseStream.Position = 0;

                // get back to first actual data line
                for (int i = 0; i < headerCheck.linesRead; i++)
                {
                    streamReader.ReadLine();
                }
                lines = 0;

                // calculate actual point data lines
                int splitCount = 0;
                while (!streamReader.EndOfStream)
                {
                    line = streamReader.ReadLine();

                    if (line.Length > 9)
                    {
                        splitCount = CharCount(line, ' ');
                        if (splitCount > 2 && splitCount < 16)
                        {
                            lines++;
                        }
                    }
                }

                Debug.Log("Found rows: " + lines);

                // reset back to start of data
                streamReader.DiscardBufferedData();
                streamReader.BaseStream.Seek(0, SeekOrigin.Begin);
                streamReader.BaseStream.Position = 0;

                // now skip header lines
                for (int i = 0; i < headerCheck.linesRead; i++)
                {
                    streamReader.ReadLine();
                }

                masterPointCount = lines;
            }


            // start saving into binary file
            var bs = new BufferedStream(new FileStream(saveFilePath, FileMode.Create));
            var writer = new BinaryWriter(bs);

            // write header
            long dataIndex = 0;
            long dataIndexRGB = 0;
            // old header
            int binaryVersion = 1;
            writer.Write(binaryVersion);
            writer.Write((System.Int32)masterPointCount);
            writer.Write(readRGB | readIntensity);

            int skippedRows = 0;
            long rowCount = 0;
            bool haveMoreToRead = true;

            // for testing loading times
            //                System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //                stopwatch.Start();

            RegexOptions options = RegexOptions.None;
            Regex regex = new Regex("[ ]{2,}", options);

            //List<Vector3> tempPointList = new List<Vector3>();
            //List<Vector3> tempColorList = new List<Vector3>();
            //Bounds bounds = new Bounds();

            // process all points, one line at a time
            while (haveMoreToRead == true)
            {

                line = streamReader.ReadLine();

                if (line != null && line.Length > 9)
                {
                    // trim duplicate spaces
                    line = line.Replace("   ", " ").Replace("  ", " ").Trim();
                    row = line.Split(' ');

                    if (row.Length > 2)
                    {
                        switch (fileFormats[fileFormat])
                        {
                            case "ASC": // ASC
                                if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                {
                                    skippedRows++;
                                    continue;
                                }
                                x = double.Parse(row[0], CultureInfo.InvariantCulture);
                                y = double.Parse(row[1], CultureInfo.InvariantCulture);
                                z = double.Parse(row[2], CultureInfo.InvariantCulture);
                                if (readRGB == true)
                                {
                                    r = LUT255[int.Parse(row[3])];
                                    g = LUT255[int.Parse(row[4])];
                                    b = LUT255[int.Parse(row[5])];
                                }
                                break;

                            case "CGO": // CGO	(counter at first line and uses comma)
                                if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                {
                                    skippedRows++;
                                    continue;
                                }
                                x = double.Parse(row[0].Replace(",", "."), CultureInfo.InvariantCulture);
                                y = double.Parse(row[1].Replace(",", "."), CultureInfo.InvariantCulture);
                                z = double.Parse(row[2].Replace(",", "."), CultureInfo.InvariantCulture);
                                break;

                            case "CATIA ASC": // CATIA ASC (with header and Point Format           = 'X %f Y %f Z %f')
                                if (IsFirstCharacter(line, '!') || IsFirstCharacter(line, '*'))
                                {
                                    skippedRows++;
                                    continue;
                                }
                                x = double.Parse(row[1], CultureInfo.InvariantCulture);
                                y = double.Parse(row[3], CultureInfo.InvariantCulture);
                                z = double.Parse(row[5], CultureInfo.InvariantCulture);
                                break;

                            case "XYZRGB":
                            case "XYZ": // XYZ RGB(INT)
                                x = double.Parse(row[0], CultureInfo.InvariantCulture);
                                y = double.Parse(row[1], CultureInfo.InvariantCulture);
                                z = double.Parse(row[2], CultureInfo.InvariantCulture);
                                if (readRGB == true)
                                {
                                    r = LUT255[int.Parse(row[3])];
                                    g = LUT255[int.Parse(row[4])];
                                    b = LUT255[int.Parse(row[5])];
                                }
                                break;

                            case "PTS": // PTS (INT) (RGB)
                                x = double.Parse(row[0].Replace(",", "."), CultureInfo.InvariantCulture);
                                y = double.Parse(row[1].Replace(",", "."), CultureInfo.InvariantCulture);
                                z = double.Parse(row[2].Replace(",", "."), CultureInfo.InvariantCulture);

                                //if (rowCount < 10) Debug.Log(row[0].Replace(",", ".") + "," + row[1].Replace(",", ".") + "," + row[2].Replace(",", ".") + "    " + x + "," + y + "," + z);

                                if (readRGB == true)
                                {
                                    if (row.Length == 7) // XYZIRGB (skip i or reflectance)
                                    {
                                        r = LUT255[int.Parse(row[4])];
                                        g = LUT255[int.Parse(row[5])];
                                        b = LUT255[int.Parse(row[6])];
                                    }
                                    else if (row.Length == 6) // XYZRGB
                                    {
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                }
                                break;

                            case "PLY (ASCII)": // PLY (ASCII)
                                x = double.Parse(row[0], CultureInfo.InvariantCulture);
                                y = double.Parse(row[1], CultureInfo.InvariantCulture);
                                z = double.Parse(row[2], CultureInfo.InvariantCulture);

                                if (readRGB == true)
                                {
                                    if (plyHasNormals == true)
                                    {
                                        r = LUT255[int.Parse(row[6])];
                                        g = LUT255[int.Parse(row[7])];
                                        b = LUT255[int.Parse(row[8])];
                                    }
                                    else if (plyHasDensity == false) // no normals or density
                                    {
                                        r = LUT255[int.Parse(row[3])];
                                        g = LUT255[int.Parse(row[4])];
                                        b = LUT255[int.Parse(row[5])];
                                    }
                                    else // no normals, but have density
                                    {
                                        r = LUT255[int.Parse(row[4])];
                                        g = LUT255[int.Parse(row[5])];
                                        b = LUT255[int.Parse(row[6])];
                                    }
                                    //a = float.Parse(row[6])/255; // TODO: alpha not supported yet
                                }
                                /*
                                }*/
                                break;

                            case "PCD (ASCII)": // PCD (ASCII)
                                x = double.Parse(row[0], CultureInfo.InvariantCulture);
                                y = double.Parse(row[1], CultureInfo.InvariantCulture);
                                z = double.Parse(row[2], CultureInfo.InvariantCulture);

                                if (readRGB == true)
                                {
                                    // TODO: would need to check both rgb formats, this is for single value only
                                    if (row.Length == 4)
                                    {
                                        var rgb = (int)decimal.Parse(row[3], System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture);

                                        r = (rgb >> 16) & 0x0000ff;
                                        g = (rgb >> 8) & 0x0000ff;
                                        b = (rgb) & 0x0000ff;

                                        r = LUT255[(int)r];
                                        g = LUT255[(int)g];
                                        b = LUT255[(int)b];
                                    }
                                    else if (row.Length == 6)
                                    {
                                        r = LUT255[int.Parse(row[3], CultureInfo.InvariantCulture)];
                                        g = LUT255[int.Parse(row[4], CultureInfo.InvariantCulture)];
                                        b = LUT255[int.Parse(row[5], CultureInfo.InvariantCulture)];
                                    }
                                }
                                else if (readIntensity == true)
                                {
                                    if (row.Length == 4) // XYZI only
                                    {
                                        r = float.Parse(row[3]);
                                        g = r;
                                        b = r;
                                    }
                                }
                                break;

                            default:
                                Debug.LogError("PointCloud2Binary" + "> Error : Unknown format");
                                break;

                        } // switch

                        x += autoOffset.x;
                        y += autoOffset.y;
                        z += autoOffset.z;


                        if (flipYZ == true)
                        {
                            var tempVal = z;
                            z = y;
                            y = tempVal;
                        }

                        // old version
                        writer.Write((float)x);
                        writer.Write((float)y);
                        writer.Write((float)z);

                        // if have color data
                        if (readRGB == true || readIntensity == true)
                        {
                            writer.Write(r);
                            writer.Write(g);
                            writer.Write(b);
                        }
                        rowCount++;
                    }
                    else
                    { // if row length
                      //Debug.Log(line);
                        skippedRows++;
                    }
                }

                // reached end or enough points
                if (rowCount >= masterPointCount || streamReader.EndOfStream)
                {

                    if (skippedRows > 0) Debug.LogWarning("Parser skipped " + skippedRows + " rows (probably bad data or comment lines in point cloud file)");
                    //Debug.Log(masterVertexCount);

                    if (rowCount < masterPointCount) // error, file ended too early, not enough points
                    {
                        Debug.LogWarning("File does not contain enough points, fixing point count to " + rowCount + " (expected : " + masterPointCount + ") - Fixing header point count.");

                        // fix header point count, not needed for v2
                        writer.BaseStream.Seek(0, SeekOrigin.Begin);
                        writer.Write(binaryVersion);
                        writer.Write((System.Int32)rowCount);

                    }
                    haveMoreToRead = false;
                }
            } // while loop reading file

            writer.Close();

            // for testing load timer
            //                stopwatch.Stop();
            //                Debug.Log("Timer: " + stopwatch.ElapsedMilliseconds + "ms"); // this one gives you the time in ms
            //                stopwatch.Reset();


            Debug.Log("PointCloud2Binary" + "> Binary file saved: " + saveFilePath + " (" + masterPointCount + " points)");
        } // using reader

        stopwatch.Stop();
        Debug.LogFormat("Timer: {0} ms", stopwatch.ElapsedMilliseconds);
        stopwatch.Reset();
    }

    int CharCount(string source, char separator)
    {
        int count = 0;
        for (int i = 0, length = source.Length; i < length; i++)
        {
            if (source[i] == separator) count++;
        }
        return count;
    }

    bool IsFirstCharacter(string source, char toFind)
    {
        if (source == null || source.Length == 0) return false;
        return source[0] == toFind;
    }

    float[] LUT255 = new float[] { 0f, 0.00392156862745098f, 0.00784313725490196f, 0.011764705882352941f, 0.01568627450980392f, 0.0196078431372549f, 0.023529411764705882f, 0.027450980392156862f, 0.03137254901960784f, 0.03529411764705882f, 0.0392156862745098f, 0.043137254901960784f, 0.047058823529411764f, 0.050980392156862744f, 0.054901960784313725f, 0.058823529411764705f, 0.06274509803921569f, 0.06666666666666667f, 0.07058823529411765f, 0.07450980392156863f, 0.0784313725490196f, 0.08235294117647059f, 0.08627450980392157f, 0.09019607843137255f, 0.09411764705882353f, 0.09803921568627451f, 0.10196078431372549f, 0.10588235294117647f, 0.10980392156862745f, 0.11372549019607843f, 0.11764705882352941f, 0.12156862745098039f, 0.12549019607843137f, 0.12941176470588237f, 0.13333333333333333f, 0.13725490196078433f, 0.1411764705882353f, 0.1450980392156863f, 0.14901960784313725f, 0.15294117647058825f, 0.1568627450980392f, 0.1607843137254902f, 0.16470588235294117f, 0.16862745098039217f, 0.17254901960784313f, 0.17647058823529413f, 0.1803921568627451f, 0.1843137254901961f, 0.18823529411764706f, 0.19215686274509805f, 0.19607843137254902f, 0.2f, 0.20392156862745098f, 0.20784313725490197f, 0.21176470588235294f, 0.21568627450980393f, 0.2196078431372549f, 0.2235294117647059f, 0.22745098039215686f, 0.23137254901960785f, 0.23529411764705882f, 0.23921568627450981f, 0.24313725490196078f, 0.24705882352941178f, 0.25098039215686274f, 0.2549019607843137f, 0.25882352941176473f, 0.2627450980392157f, 0.26666666666666666f, 0.27058823529411763f, 0.27450980392156865f, 0.2784313725490196f, 0.2823529411764706f, 0.28627450980392155f, 0.2901960784313726f, 0.29411764705882354f, 0.2980392156862745f, 0.30196078431372547f, 0.3058823529411765f, 0.30980392156862746f, 0.3137254901960784f, 0.3176470588235294f, 0.3215686274509804f, 0.3254901960784314f, 0.32941176470588235f, 0.3333333333333333f, 0.33725490196078434f, 0.3411764705882353f, 0.34509803921568627f, 0.34901960784313724f, 0.35294117647058826f, 0.3568627450980392f, 0.3607843137254902f, 0.36470588235294116f, 0.3686274509803922f, 0.37254901960784315f, 0.3764705882352941f, 0.3803921568627451f, 0.3843137254901961f, 0.38823529411764707f, 0.39215686274509803f, 0.396078431372549f, 0.4f, 0.403921568627451f, 0.40784313725490196f, 0.4117647058823529f, 0.41568627450980394f, 0.4196078431372549f, 0.4235294117647059f, 0.42745098039215684f, 0.43137254901960786f, 0.43529411764705883f, 0.4392156862745098f, 0.44313725490196076f, 0.4470588235294118f, 0.45098039215686275f, 0.4549019607843137f, 0.4588235294117647f, 0.4627450980392157f, 0.4666666666666667f, 0.47058823529411764f, 0.4745098039215686f, 0.47843137254901963f, 0.4823529411764706f, 0.48627450980392156f, 0.49019607843137253f, 0.49411764705882355f, 0.4980392156862745f, 0.5019607843137255f, 0.5058823529411764f, 0.5098039215686274f, 0.5137254901960784f, 0.5176470588235295f, 0.5215686274509804f, 0.5254901960784314f, 0.5294117647058824f, 0.5333333333333333f, 0.5372549019607843f, 0.5411764705882353f, 0.5450980392156862f, 0.5490196078431373f, 0.5529411764705883f, 0.5568627450980392f, 0.5607843137254902f, 0.5647058823529412f, 0.5686274509803921f, 0.5725490196078431f, 0.5764705882352941f, 0.5803921568627451f, 0.5843137254901961f, 0.5882352941176471f, 0.592156862745098f, 0.596078431372549f, 0.6f, 0.6039215686274509f, 0.6078431372549019f, 0.611764705882353f, 0.615686274509804f, 0.6196078431372549f, 0.6235294117647059f, 0.6274509803921569f, 0.6313725490196078f, 0.6352941176470588f, 0.6392156862745098f, 0.6431372549019608f, 0.6470588235294118f, 0.6509803921568628f, 0.6549019607843137f, 0.6588235294117647f, 0.6627450980392157f, 0.6666666666666666f, 0.6705882352941176f, 0.6745098039215687f, 0.6784313725490196f, 0.6823529411764706f, 0.6862745098039216f, 0.6901960784313725f, 0.6941176470588235f, 0.6980392156862745f, 0.7019607843137254f, 0.7058823529411765f, 0.7098039215686275f, 0.7137254901960784f, 0.7176470588235294f, 0.7215686274509804f, 0.7254901960784313f, 0.7294117647058823f, 0.7333333333333333f, 0.7372549019607844f, 0.7411764705882353f, 0.7450980392156863f, 0.7490196078431373f, 0.7529411764705882f, 0.7568627450980392f, 0.7607843137254902f, 0.7647058823529411f, 0.7686274509803922f, 0.7725490196078432f, 0.7764705882352941f, 0.7803921568627451f, 0.7843137254901961f, 0.788235294117647f, 0.792156862745098f, 0.796078431372549f, 0.8f, 0.803921568627451f, 0.807843137254902f, 0.8117647058823529f, 0.8156862745098039f, 0.8196078431372549f, 0.8235294117647058f, 0.8274509803921568f, 0.8313725490196079f, 0.8352941176470589f, 0.8392156862745098f, 0.8431372549019608f, 0.8470588235294118f, 0.8509803921568627f, 0.8549019607843137f, 0.8588235294117647f, 0.8627450980392157f, 0.8666666666666667f, 0.8705882352941177f, 0.8745098039215686f, 0.8784313725490196f, 0.8823529411764706f, 0.8862745098039215f, 0.8901960784313725f, 0.8941176470588236f, 0.8980392156862745f, 0.9019607843137255f, 0.9058823529411765f, 0.9098039215686274f, 0.9137254901960784f, 0.9176470588235294f, 0.9215686274509803f, 0.9254901960784314f, 0.9294117647058824f, 0.9333333333333333f, 0.9372549019607843f, 0.9411764705882353f, 0.9450980392156862f, 0.9490196078431372f, 0.9529411764705882f, 0.9568627450980393f, 0.9607843137254902f, 0.9647058823529412f, 0.9686274509803922f, 0.9725490196078431f, 0.9764705882352941f, 0.9803921568627451f, 0.984313725490196f, 0.9882352941176471f, 0.9921568627450981f, 0.996078431372549f, 1f };

}
