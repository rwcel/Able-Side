using UnityEngine.Networking;

namespace Gpm.Common.Indicator.Internal
{
    public class LaunchingRequest
    {
        public UnityWebRequest Request(string appKey = Launching.APP_KEY, string subKey = null)
        {
            string url = string.Format("{0}/{1}/appkeys/{2}/configurations", Launching.URI, Launching.VERSION, appKey);
            if (string.IsNullOrEmpty(subKey) == false)
            {
                url += "?subKey=launching." + subKey;
            }
            var request = UnityWebRequest.Get(url);
            request.method = UnityWebRequest.kHttpVerbGET;

            return request;
        }
    }
}