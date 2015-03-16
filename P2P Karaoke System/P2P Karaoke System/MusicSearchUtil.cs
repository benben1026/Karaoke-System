using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* This file contains the MusicSearchUtil class, which is a static class with only one method.
 * (I don't know whether this is good practice, but I can't think of other ways to do it)

 * The method assumes using a searchString and a list storing musicData as input.
 * (I suppose the UI uses a list to store data from database and this method does not need to open database again, tell me if I am wrong)
 * It returns another list of MusicData
 * The UI should be able to display the music according to that list.
*/

namespace P2P_Karaoke_System {
    static class MusicSearchUtil {

        public static List<MusicCopy> SearchedMusicList(String searchString, List<MusicCopy> inputMusicList)
        {
            int size = inputMusicList.Count();
            bool[] musicIsWantedRecord = new bool[size];  //Store the result of searching
            List<MusicCopy> outputMusicList = new List<MusicCopy>();

            for (int i = 0; i < size; i++) {
                musicIsWantedRecord[i] = true;
            }

            String[] searchWords = searchString.Split(' ');
            foreach (String s in searchWords) {
                for (int i = 0; i < size; i++) {
                	// calculate the relevancy
                	if(inputMusicList[i].AudioData.Title.Contains(s)){
                		inputMusicList[i].Relevancy ++;
                	}
                    if (inputMusicList[i].AudioData.Artist.Contains(s))
                    {
                		inputMusicList[i].Relevancy ++;
                	}

                    musicIsWantedRecord[i] = musicIsWantedRecord[i] && ( inputMusicList[i].Relevancy != 0 );
                    //If music data does not contain keyword(s), i.e. relevancy = 0, musicIsWantedRecord[i] will be false
                }
            }
            for (int i = 0; i < size; i++) {
                if (musicIsWantedRecord[i]) {
                    outputMusicList.Add(inputMusicList[i]);
                }
            }
            return outputMusicList;
        }

    }
}
