using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CrisClient
{
    class Program
    {
        // authentication constants
        private const string UserName = "the MSA you used to login to customspeech.ai portal";

        // internal api key
        private const string ApiKey = "<the API key you generated at customspeech.ai portal>";
        
        // internal sub key
        private const string SubscriptionKey = "<your Speech[Preview] subscription key>";

        private const string HostName = "cris.ai";
        private const int Port = 443;

        // recordings and locale
        private const string Locale = "en-US";
        private const string RecordingsBlobUri = "<URI pointing to an audio file stored Azure blob>";

        // adapted model Ids
        private static Guid AdaptedAcousticId = new Guid("<id of the custom acoustic model");
        private static Guid AdaptedLanguageId = new Guid("<ID of the custom language model>");

        static void Main(string[] args)
        {
            TranscribeAsync().Wait();
        }

        static async Task TranscribeAsync()
        {
            Console.WriteLine("Starting transcriptions client...");

            // create the client object and authenticate
            var client = await CrisClient.CreateApiV1ClientAsync(UserName, ApiKey, HostName, Port).ConfigureAwait(false);

            // get all transcriptions for the user
            var transcriptions = await client.GetTranscriptionsAsync().ConfigureAwait(false);

            Console.WriteLine("Deleting all existing completed transcriptions.");
            // delete all pre-existing completed transcriptions. If transcriptions are still running or not started, they will not be deleted
            foreach (var item in transcriptions)
            {
                // delete a transcription
                await client.DeleteTranscriptionAsync(item.Id).ConfigureAwait(false);
            }

            Console.WriteLine("Creating transcriptions.");            
            var transcriptionLocation = await client.PostTranscriptionAsync(Locale, SubscriptionKey, new Uri(RecordingsBlobUri), new[] { AdaptedAcousticId, AdaptedLanguageId }).ConfigureAwait(false);

            // if you want to use baseline models then simply comment out the above line use the one below
            //var transcriptionLocation = await client.PostTranscriptionAsync(Locale, SubscriptionKey, new Uri(RecordingsBlobUri)).ConfigureAwait(false);

            // get the transcription Id from the location URI
            var createdTranscriptions = new List<Guid>();
            createdTranscriptions.Add(new Guid(transcriptionLocation.ToString().Split('/').LastOrDefault()));

            Console.WriteLine("Checking status.");
            // check for the status of our transcriptions every 30 sec. (can also be 1, 2, 5 min depending on usage)
            int completed = 0, running = 0, notStarted = 0;
            while (completed < 2)
            {
                // get all transcriptions for the user
                transcriptions = await client.GetTranscriptionsAsync().ConfigureAwait(false);

                completed = 0; running = 0; notStarted = 0;
                // for each transcription in the list we check the status
                foreach (var transcription in transcriptions)
                {
                    switch(transcription.Status)
                    {
                        case "Failed":
                        case "Succeeded":
                            // we check to see if it was one of the transcriptions we created from this client.
                            if (!createdTranscriptions.Contains(transcription.Id))
                            {
                                // not creted form here, continue
                                continue;
                            }
                            completed++;
                            
                            // if the transcription was successfull, check the results
                            if (transcription.Status == "Succeeded")
                            {
                                var resultsUri = transcription.ResultsUrls["channel_0"];

                                WebClient webClient = new WebClient();

                                var filename = Path.GetTempFileName();
                                webClient.DownloadFile(resultsUri, filename);

                                var results = File.ReadAllText(filename);
                                Console.WriteLine("Transcription succedded. Results: ");
                                Console.WriteLine(results);
                            }
                            break;

                        case "Running":
                            running++;
                            break;

                        case "NotStarted":
                            notStarted++;
                            break;
                    }
                }

                Console.WriteLine(string.Format("Transcriptions status: {0} completed, {1} running, {2} not started yet", completed, running, notStarted));
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
