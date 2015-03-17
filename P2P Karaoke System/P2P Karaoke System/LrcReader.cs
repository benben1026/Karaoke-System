using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

/* This file contains the LrcReader class, which is designed to be used by UI
 * Also there is a Lyrics class, which is just used by the LrcReader class
 * 
 * To construct a LrcReader object, simply give it a filename: eg. LrcReader reader = new LrcReader("歲月如歌.lrc");
 * After creating an object, you can use StartStopwatch(), StopStopwatch(), ResetStopwatch(), RestartStopwatch() to control the stopwatch inside the LrcReader
 * You can also set the Stopwatch to a particular time by SetStopwatch(int milliseconds).
 * You can get the current lyrics by the GetCurrentLyrics(), it returns a string at a particular time.

*/
namespace P2P_Karaoke_System {
    class LrcReader {
        private string filename;
        private StreamReader sr;
        private bool fileLoadedSuccessfully;
        private List<Lyrics> lyricsList;

        private StopwatchWithOffset sw;
        private Lyrics currentLyrics;

        public LrcReader(String filename) {

            if (filename.Contains(".lrc")) {
                this.filename = filename;
            }
            try {
                sr = new StreamReader(this.filename);
                fileLoadedSuccessfully = true;
                lyricsList = new List<Lyrics>();
                StoreLyrics();
                Console.WriteLine("Sucess loading:" + this.filename);

                sw = new StopwatchWithOffset(0);

                sr.Close();
            } catch (IOException e) {
                Console.WriteLine(e);
            } catch (ArgumentNullException e) {
                Console.WriteLine(e);
            }

        }

        private void StoreLyrics() {
            String s;
            String content;
            String mmstring;
            String ssstring;
            int mm;
            double ss;

            while ((s = sr.ReadLine()) != null) {
                if (Char.IsNumber(s[1])) {
                    //is lyrics, do something

                    do {
                        mmstring = s.Substring(s.IndexOf('[') + 1, s.IndexOf(':') - s.IndexOf('[') - 1);
                        mm = Int32.Parse(mmstring);

                        ssstring = s.Substring(s.IndexOf(':') + 1, s.IndexOf(']') - s.IndexOf(':') - 1);
                        ss = Double.Parse(ssstring);

                        content = s.Substring(s.LastIndexOf(']') + 1);
                        lyricsList.Add(new Lyrics(mm * 60 + ss, content));
                        s = s.Substring(s.IndexOf(']') + 1);

                    } while (s.Contains('[')); //Read again if there is more than one [] on one line
                }
            }
            lyricsList.Sort();
            currentLyrics = lyricsList[0];
        }

        public void PrintLyrics() {
            foreach (Lyrics t in lyricsList) {
                t.PrintLyrics();
            }
        }

        //The methods that can be used by UI

        public bool FileLoaded() {
            return fileLoadedSuccessfully;
        }

        public void StartStopwatch() {
            sw.Start();
        }

        public void StopStopwatch() {
            sw.Stop();
        }

        public void ResetStopwatch() {
            sw.Reset();
        }

        public void RestartStopwatch() {
            sw.Restart();
        }

        public void SetStopwatch(int milliseconds) {
            sw = new StopwatchWithOffset(milliseconds);
        }

        public String GetCurrentLyrics() {
            if (fileLoadedSuccessfully) {
                int n = 0;
                while ((n < lyricsList.Count - 1) && lyricsList[n + 1].GetLyricsMillisecond() < sw.ElapsedMilliseconds) {
                    n++;
                }
                return lyricsList[n].GetLyricsContent();
            } else {
                return " ";
            }
            
        }

        public String GetLyricsByTimeWithOffset(int milliseconds, int offset){
            if (fileLoadedSuccessfully) {
                int n = 0;
                while ((n < lyricsList.Count - 1) && lyricsList[n + 1].GetLyricsMillisecond() < milliseconds) {
                    n++;
                }

                return (n + offset >= 0 && n + offset <= lyricsList.Count - 1) ? lyricsList[n + offset].GetLyricsContent() : " ";
            } else {
                return " ";
            }
        } 

        public long GetTimeCount() {
            return sw.ElapsedMilliseconds;
        }
    }

    //==============End of LrcReader class==========
    //==============Other class below==============


    internal class Lyrics : IComparable {
        private int lyricsMilliSecond;
        private String lyricsContent;

        public Lyrics(double d, String s) {
            lyricsMilliSecond = (int)(d * 1000);
            lyricsContent = s;
        }

        public int GetLyricsMillisecond() {
            return lyricsMilliSecond;
        }

        public String GetLyricsContent() {
            return lyricsContent;
        }

        public void PrintLyrics() {
            Console.WriteLine(lyricsMilliSecond + " " + lyricsContent);
        }

        public int CompareTo(Object obj) {
            Lyrics ly = obj as Lyrics;
            return lyricsMilliSecond.CompareTo(ly.GetLyricsMillisecond());
        }
    }

    internal class StopwatchWithOffset : Stopwatch {
        private long StartOffsetMillisecond;

        public StopwatchWithOffset(long millisecond) {
            StartOffsetMillisecond = millisecond;
        }

        public new long ElapsedMilliseconds {
            get {
                return base.ElapsedMilliseconds + StartOffsetMillisecond;
            }
        }
    }

}
