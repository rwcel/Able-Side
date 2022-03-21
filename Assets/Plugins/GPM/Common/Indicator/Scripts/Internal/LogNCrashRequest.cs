using UnityEngine.Networking;

namespace Gpm.Common.Indicator.Internal
{
    public class LogNCrashRequest
    {
        protected LaunchingInfo.Launching.Indicator.Zone indicatorInfo;

        public LogNCrashRequest(LaunchingInfo.Launching.Indicator.Zone indicatorInfo)
        {
            this.indicatorInfo = indicatorInfo;
        }

        public UnityWebRequest Request(QueueItem queueItem)
        {
            string json =
                        IndicatorField.CreateJson(
                            indicatorInfo.appKey,
                            indicatorInfo.logVersion,
                            queueItem.serviceName,
                            queueItem.serviceVersion,
                            queueItem.body,
                            queueItem.customData);

            return Request(json);
        }

        public UnityWebRequest Request(string json)
        {
            var request = UnityWebRequest.Post(string.Format("{0}/{1}/log", indicatorInfo.url, indicatorInfo.logVersion), json);

            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.method = UnityWebRequest.kHttpVerbPOST;
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }
    }
}