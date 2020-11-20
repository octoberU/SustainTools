using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using AudicaTools;

namespace SustainTools
{
    class Program
    {
        public static string workingDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        static void Main(string[] args)
        {
            //args = new string[] { @"C:\Users\adamk\source\repos\SustainTool\bin\Release\netcoreapp3.1\BeforeDawn-octo.audica" };
            try
            {
                foreach (var path in args)
                {
                    if (path.Contains(".audica"))
                    {
                        var audica = new Audica(path);
                        string audicaDirectory = Path.GetDirectoryName(path);
                        Console.WriteLine("Starting conversion: " + path);
                        string tempPath = Path.Combine(Program.workingDirectory, "SUSTAINTOOLSTEMP");
                        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true); // Clean up previous failed attempt
                        Directory.CreateDirectory(tempPath);
                        string tempSongPath = Path.Combine(tempPath, "song.ogg");
                        audica.song.ExportToOgg(tempSongPath);
                        
                        Process ffmpeg = new Process();
                        ffmpeg.StartInfo.FileName = Path.Join(Program.workingDirectory, "ffmpeg.exe");
                        ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        ffmpeg.StartInfo.UseShellExecute = false;
                        ffmpeg.StartInfo.RedirectStandardOutput = false;

                        Console.WriteLine("Converting to mp3");
                        //Convert to mp3 because Audica breaks when converting ogg to ogg.
                        ffmpeg.StartInfo.Arguments = $"-y -i  \"{tempSongPath}\" {tempSongPath.Replace(".ogg", ".mp3")}";
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();

                        string filterString = "-ac 1 -af \"chorus = in_gain = 0.5:out_gain = 0.9:delays = ' 50':decays = '  0.4':speeds = '  0.25':depths = '  2', acrusher = samples = 10:bits = 16, flanger = delay = 0:depth = 2:regen = 0:width = 71:shape = sinusoidal:phase = 25:interp = linear, lowpass = f = 600, crystalizer = i = 10,equalizer = f = 1000:t = q:w = 1:g = 2,equalizer = f = 300:t = q:w = 2:g = -5\"";
                        
                        Console.WriteLine("Running filters");
                        ffmpeg.StartInfo.Arguments = $"-y -i {tempSongPath.Replace(".ogg", ".mp3")} {filterString} {Path.Combine(tempPath, "song_sustain_l.ogg")}";
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();

                        Process ogg2mogg = new Process();
                        ogg2mogg.StartInfo.FileName = Path.Join(Program.workingDirectory, "ogg2mogg.exe");
                        ogg2mogg.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                        ogg2mogg.StartInfo.UseShellExecute = false;
                        ogg2mogg.StartInfo.RedirectStandardOutput = false;

                        Console.WriteLine("Converting to mogg");
                        string moggPath = Path.Combine(tempPath, "song_sustain_l.mogg");
                        ogg2mogg.StartInfo.Arguments = $"{Path.Combine(tempPath, "song_sustain_l.ogg")} {moggPath}";
                        ogg2mogg.Start();
                        ogg2mogg.WaitForExit();

                        using (FileStream newSustainMogg = new FileStream(moggPath, FileMode.Open))
                        {
                            audica.songSustainL = new Mogg(newSustainMogg);
                            audica.songSustainR = audica.songSustainL;
                        }
                        

                        audica.desc.sustainSongRight = "song_sustain_r.moggsong";
                        audica.desc.sustainSongLeft = "song_sustain_l.moggsong";

                        Console.WriteLine("Exporting audica");
                        audica.moggSong.pan = new MoggSong.MoggVol(-1f, 1f);
                        string newAudicaPath = path.Replace(".audica", "_s.audica");
                        audica.Export(newAudicaPath);
                        
                        Console.WriteLine("Adding moggsongs");

                        //Add missing moggsongs for sustains
                        using (FileStream zipToOpen = new FileStream(newAudicaPath, FileMode.Open))
                        {
                            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                            {
                                ZipArchiveEntry moggSongL = archive.CreateEntry("song_sustain_l.moggsong");
                                using (StreamWriter writer = new StreamWriter(moggSongL.Open()))
                                {
                                    writer.BaseStream.Write(Properties.Resources.song_sustain_l);
                                }
                                ZipArchiveEntry moggSongR = archive.CreateEntry("song_sustain_r.moggsong");
                                using (StreamWriter writer = new StreamWriter(moggSongR.Open()))
                                {
                                    writer.BaseStream.Write(Properties.Resources.song_sustain_r);
                                }
                            }
                        }

                        if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true); // Clean up 


                        
                        Console.WriteLine("Done:" + newAudicaPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Sustain tool failed. Create an issue with your error on GitHub");
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }
    }
}
