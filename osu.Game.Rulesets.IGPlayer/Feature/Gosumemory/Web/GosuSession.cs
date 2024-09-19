using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using NetCoreServer;

namespace osu.Game.Rulesets.IGPlayer.Feature.Gosumemory.Web;

public class GosuSession : WsSession
{
    private readonly WebSocketLoader.GosuServer gosuServer;

    public GosuSession(WebSocketLoader.GosuServer server)
        : base(server)
    {
        this.gosuServer = server;
    }

    protected override void OnReceivedRequest(HttpRequest request)
    {
        string path = request.Url ?? "/";

        // 跳过favicon.ico
        if (path == "/favicon.ico")
        {
            var response = new HttpResponse();
            response.SetBegin(404);

            this.SendResponse(response);
            return;
        }

        if (!path.EndsWith("/ws", StringComparison.Ordinal)
            && !path.EndsWith("/json", StringComparison.Ordinal))
        {
            //Logging.Log("Received FILE？ request " + request.Url);

            HttpResponse response = new HttpResponse();
            response.SetBegin(200);

            const string cache_control_str = "public, max-age=0";
            response.SetHeader("Cache-Control", cache_control_str)
                    .SetHeader("Access-Control-Allow-Origin", "*");

            string getLinkUrl(string link, string name) => $"<a href=\"{link}\">{name}</a>";

            var storage = gosuServer.GetStorage();

            // 因为不知道怎么让他显示目录所以只好自己搓了 UwU
            string htmlCode = "<html>";

            htmlCode += "<head>"
                        + "<meta charset=\"utf-8\">"
                        + "<meta name=\"color-scheme\" content=\"light dark\">"
                        + "<meta name=\"google\" value=\"notranslate\">"
                        + "</head>";

            if (storage == null)
            {
                htmlCode += "<h1>Gosu文件服务尚未初始化完毕，请稍后再来</h1>"
                            + "<h1>Gosu file service is not initialized yet, please come back later.</h1>";
            }
            else
            {
                string urlPath = path.StartsWith('/')
                    ? path.Remove(0, 1) // 将其变为相对目录
                    : path;

                // storagePath始终不为空
                if (string.IsNullOrEmpty(urlPath)) urlPath = ".";

                // 将gosu_statics和urlPath混合，得到我们要的相对存储路径
                string storagePath = Path.Combine("gosu_statics", urlPath);

                // 目标存储的绝对位置
                string targetFilePath = storage.GetFullPath(storagePath);

                //Logging.Log("URLPath is " + urlPath);

                // 处理Songs
                if (urlPath.StartsWith("Songs", StringComparison.Ordinal))
                {
                    string[] split = urlPath.Split("/", 2);
                    var fileResponse = new HttpResponse();

                    if (split.Length < 2)
                    {
                        Logging.Log("Illegal argument, not processing...");
                        fileResponse.SetBegin(400);
                        this.SendResponse(fileResponse);

                        return;
                    }

                    string fileName = split[1].Split("?", 2)[0];

                    byte[] content = gosuServer.FindStaticOrAsset(fileName) ?? new byte[] { };

                    if (content.Length == 0)
                    {
                        response.SetBegin(404);
                        //Logging.Log("404 File Length is " + response.BodyLength);

                        this.SendResponse(response);

                        return;
                    }

                    fileResponse.SetBegin(200);
                    response.SetHeader("Cache-Control", cache_control_str)
                            .SetHeader("Access-Control-Allow-Origin", "*");

                    response.SetBody(content);

                    //Logging.Log("File Length is " + response.BodyLength);

                    this.SendResponse(response);
                    return;
                }

                // 如果要访问文件, 那么不要进行处理
                if (File.Exists(targetFilePath))
                {
                    var fileResponse = new HttpResponse();
                    byte[] content = gosuServer.FindStaticOrAsset(targetFilePath) ?? new byte[] { };

                    if (content.Length == 0)
                    {
                        fileResponse.SetBegin(404);
                        this.SendResponse(response);
                        return;
                    }

                    fileResponse.SetBegin(200);
                    response.SetHeader("Cache-Control", cache_control_str)
                            .SetHeader("Access-Control-Allow-Origin", "*");

                    response.SetHeader("Content-Type", MimeTypeMap.GetMimeType(targetFilePath));

                    response.SetBody(File.ReadAllBytes(targetFilePath));

                    //Logging.Log("File Length is " + response.BodyLength);

                    this.SendResponse(response);
                    return;
                }

                // 只当目录存在时遍历其中的内容
                if (Path.Exists(targetFilePath))
                {
                    try
                    {
                        // 反之，添加所有文件和目录的超链接
                        var localStorage = storage.GetStorageForDirectory(storagePath);

                        foreach (string directory in localStorage.GetDirectories("."))
                            htmlCode += getLinkUrl($"/{urlPath}/{directory}", directory) + "<br>";

                        foreach (string file in localStorage.GetFiles("."))
                            htmlCode += getLinkUrl($"/{urlPath}/{file}", file) + "<br>";
                    }
                    catch (Exception e)
                    {
                        Logging.Log($"Error occurred presenting directory information! {e.Message}");
                        Logging.Log($"{e.StackTrace ?? "<No stacktrace>"}");
                    }
                }
                else
                {
                    htmlCode += getLinkUrl("/", "[根目录]")
                                + "<br><br>"
                                + "404 Not Found UwU";
                }
            }

            htmlCode += "</html>";

            response.SetBody(htmlCode);
            this.SendResponse(response);

            //Logging.Log("Sending " + response.Body);
        }
        else
        {
            //Logging.Log("Received WS OR JSON request " + request.Url);

            base.OnReceivedRequest(request);
        }
    }

    public override void OnWsConnected(HttpRequest request)
    {
        Logging.Log($"WebSocket session with Id {Id} connected!");
    }

    public override void OnWsDisconnected()
    {
        Logging.Log($"Chat WebSocket session with Id {Id} disconnected!");
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        string message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
        Logging.Log("WebSocket Incoming: " + message);
    }

    public override void OnWsError(string error)
    {
        Logging.Log("WS ERRORED: " + error);
    }

    public override void OnWsError(SocketError error)
    {
        Logging.Log("WS ERRORED: " + error);
    }

    protected override void OnError(SocketError error)
    {
        Logging.Log($"Chat WebSocket session caught an error with code {error}");
    }
}
