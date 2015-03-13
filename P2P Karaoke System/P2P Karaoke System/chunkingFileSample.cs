using System;
using System.IO;

namespace TEST
{

    public class chunkFile
    {

        public static void testMain()
        {
            
            int fsize = 2009081;
            // we need to know the file size in bytes ahead
            
            FileStream fs1 = new FileStream("E:\\music1.wav", FileMode.Open);
            FileStream fs2 = new FileStream("E:\\music2.wav", FileMode.Open);
            FileStream fs3 = new FileStream("E:\\music3.wav", FileMode.Open);
            FileStream fs4 = new FileStream("E:\\music.wav", FileMode.Create);
            // in p2p, we need to open different files in different PCs
            // fs4 is the local file

            byte[] byData = new byte[fsize];
            int set1, set2;
            set1 = fsize / 3;
            set2 = fsize / 3;
            // chunking into 3 parts
          
            fs1.Read(byData, 0, set1);
            fs1.Read(byData, set1, set2);
            fs1.Read(byData, (set1 + set2), (fsize - set1 - set2));
            fs4.Write(byData, 0, fsize);

            Console.ReadLine();
            return;
        }

    }
}