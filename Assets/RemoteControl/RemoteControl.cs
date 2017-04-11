using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Text;
using System;
using System.Net;
#if UNITY_UWP
using IotWeb.Common.Http;
using IotWeb.Common.Util;
using IotWeb.Server;
using Windows.Storage;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif

public class RemoteControl : MonoBehaviour
{

    /* Singleton stuff */
    const int port = 8080;
    public static RemoteControl instance = null;

    private RemoteControl()
    {
#if UNITY_UWP
        wsHandler = new RemoteControlWSHandler();
#endif
    }

    public void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

#if UNITY_UWP
    HttpServer server;
    RemoteControlWSHandler wsHandler;
#endif

    // Use this for initialization
    void Start()
    {
#if UNITY_UWP
        server = new IotWeb.Server.HttpServer(port);
        server.AddHttpRequestHandler("/", new RemoteControlHTTPHandler(port));
        server.AddWebSocketRequestHandler("/rc/", wsHandler);
        server.Start();
#endif
    }

    // Update is called once per frame
    void Update()
    {
        // send updates here
#if UNITY_UWP
        wsHandler.FireChanges();
#endif
    }

    public void AddParam(string name, bool b, RemoteControlInputBool.ChangeHandler onchange)
    {
#if UNITY_UWP
        wsHandler.paramList.Add(new RemoteControlInputBool(name, b, onchange));
        if (wsHandler.connections.Count > 0)
        {
            wsHandler.SendParamList();
        }
#endif
    }

    public void AddParam(string name, double val, RemoteControlInputDouble.ChangeHandler onchange, double min = 0, double max = 100, double step = 1)
    {
#if UNITY_UWP
        wsHandler.paramList.Add(new RemoteControlInputDouble(name, val, onchange, min, max, step));
        if (wsHandler.connections.Count > 0)
        {
            wsHandler.SendParamList();
        }
#endif
    }

    public void AddParam(string name, Color c, RemoteControlInputColor.ChangeHandler onchange)
    {
#if UNITY_UWP
        wsHandler.paramList.Add(new RemoteControlInputColor(name, c, onchange));
        if (wsHandler.connections.Count > 0)
        {
            wsHandler.SendParamList();
        }
#endif
    }

    public void AddParam(string name, string text, RemoteControlInputString.ChangeHandler onchange)
    {
#if UNITY_UWP
        wsHandler.paramList.Add(new RemoteControlInputString(name, text, onchange));
        if (wsHandler.connections.Count > 0)
        {
            wsHandler.SendParamList();
        }
#endif
    }

    public void UpdateParam(string name, bool val)
    {
#if UNITY_UWP
        RemoteControlInputBool param = new RemoteControlInputBool(name, val, null);
        RemoteControlMessage msg = new RemoteControlMessage("update_param", JsonUtility.ToJson(param));
        wsHandler.OnDataReceived(null, JsonUtility.ToJson(msg));
#endif
    }

    public void UpdateParam(string name, double val)
    {
#if UNITY_UWP
        RemoteControlInputDouble param = new RemoteControlInputDouble(name, val, null);
        RemoteControlMessage msg = new RemoteControlMessage("update_param", JsonUtility.ToJson(param));
        wsHandler.OnDataReceived(null, JsonUtility.ToJson(msg));
#endif
    }

    public void UpdateParam(string name, Color val)
    {
#if UNITY_UWP
        RemoteControlInputColor param = new RemoteControlInputColor(name, val, null);
        RemoteControlMessage msg = new RemoteControlMessage("update_param", JsonUtility.ToJson(param));
        wsHandler.OnDataReceived(null, JsonUtility.ToJson(msg));
#endif
    }

    public void UpdateParam(string name, string val)
    {
#if UNITY_UWP
        RemoteControlInputString param = new RemoteControlInputString(name, val, null);
        RemoteControlMessage msg = new RemoteControlMessage("update_param", JsonUtility.ToJson(param));
        wsHandler.OnDataReceived(null, JsonUtility.ToJson(msg));
#endif
    }
}

#if UNITY_UWP
class RemoteControlWSHandler : IWebSocketRequestHandler
{
    public RemoteControlParams paramList;
    public List<WebSocket> connections;

    public RemoteControlWSHandler()
    {
        paramList = new RemoteControlParams();
        connections = new List<WebSocket>();
    }

    public bool WillAcceptRequest(string uri, string protocol)
    {
        return (uri.Length == 0) && (protocol == "remote-control");
    }

    public void Connected(WebSocket socket)
    {
        socket.DataReceived += OnDataReceived;
        socket.ConnectionClosed += OnConnectionClosed;
        connections.Add(socket);
    }

    public void OnDataReceived(WebSocket socket, string frame)
    {
        RemoteControlMessage msg = JsonUtility.FromJson<RemoteControlMessage>(frame);
        if (msg.type == "get_param_list")
        {
            SendParamList();
        }
        else if (msg.type == "update_param")
        {
            paramList.UpdateParam(msg.data);
            // update the other clients
            foreach (WebSocket s in connections)
            {
                if (s != socket)
                {
                    s.Send(frame);
                }
            }
        }
        else if (msg.type == "save_preset")
        {
            SaveParams(msg.data);
        }
        else if (msg.type == "load_preset")
        {
            LoadParams(msg.data);
        }
        else if (msg.type == "delete_preset")
        {
            DeletePreset(msg.data);
        }
        else if (msg.type == "get_preset_list")
        {
            SendPresetList();
        }
    }

    void OnConnectionClosed(WebSocket socket)
    {
        connections.Remove(socket);
    }

    public void SendParamList()
    {
        RemoteControlMessage response = new RemoteControlMessage("set_param_list", JsonUtility.ToJson(paramList));
        string frame = JsonUtility.ToJson(response);
        foreach (WebSocket s in connections)
        {
            s.Send(frame);
        }
    }

    public void FireChanges()
    {
        paramList.FireChanges();
    }

    public void SaveParams(string presetName)
    {
        string data = JsonUtility.ToJson(paramList);
        RemoteControlUtils.SavePreset(presetName, data);
        SendPresetList();
    }

    public void LoadParams(string presetName)
    {
        string data = RemoteControlUtils.LoadPreset(presetName);
        RemoteControlParams loadedParams = JsonUtility.FromJson<RemoteControlParams>(data);
        paramList.LoadValues(loadedParams);
        SendParamList();
    }

    public void DeletePreset(string presetName)
    {
        RemoteControlUtils.DeletePreset(presetName);
        SendPresetList();
    }

    public void SendPresetList()
    {
        PresetList list = new PresetList(RemoteControlUtils.GetPresetList());
        RemoteControlMessage msg = new RemoteControlMessage("set_preset_list", JsonUtility.ToJson(list));
        string frame = JsonUtility.ToJson(msg);
        foreach (WebSocket s in connections)
        {
            s.Send(frame);
        }
    }
}

public class RemoteControlHTTPHandler : IotWeb.Common.Http.IHttpRequestHandler
{
    int port;

    public RemoteControlHTTPHandler(int port)
    {
        this.port = port;
    }

    public void HandleRequest(string uri, HttpRequest request, HttpResponse response, HttpContext context)
    {
        if (request.Method != HttpMethod.Get)
            throw new HttpMethodNotAllowedException();

        string file = (uri=="")?"index.html":uri;
        Debug.Log("requester URI: '"+file+"'");
        string text;
        try
        {
            text = GetFileContents(file);
        }
        catch (Exception e)
        {
            response.ResponseCode = HttpResponseCode.NotFound;
            return;
        }

        if (file == "index.html")
        {
            // replace {{websocket_server_address}} with the address of our websocket server for fast connection
            text = text.Replace("{{websocket_server_address}}", "ws://"+RemoteControlUtils.GetLocalIp()+":"+port+"/rc/");
        }
        UTF8Encoding utf8 = new UTF8Encoding();
        var responseBytes = utf8.GetBytes(text);

        
        if (file.Contains(".html"))
        {
            response.Headers[HttpHeaders.ContentType] = "text/html; charset=utf-8";
        }
        else if (file.Contains(".js"))
        {
            response.Headers[HttpHeaders.ContentType] = "application/javascript";
        }
        else if (file.Contains(".css"))
        {
            response.Headers[HttpHeaders.ContentType] = "text/css";
        }
        else
        {
            response.Headers[HttpHeaders.ContentType] = "text/plain";
        }


        response.Content.Write(responseBytes, 0, responseBytes.Length);
    }

    public string GetFileContents(string path)
    {
        // read response html from file
        Task<string> task = GetTextFileAsync(path);
        return task.Result;
    }

    private async Task<string> GetTextFileAsync(string path)
    {
        try
        {
            StorageFile textFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Data/StreamingAssets/RemoteControl/" + path));
            return await FileIO.ReadTextAsync(textFile);
        }
        catch (Exception e)
        {
            Debug.Log("exception while reading text file: " + e);
            throw new HttpNotFoundException();
        }
    }
}

[System.Serializable]
public class RemoteControlMessage
{
    public string type;
    public string data;
    public RemoteControlMessage(string t, string d)
    {
        type = t;
        data = d;
    }
}

[System.Serializable]
public class RemoteControlParams
{
    public List<RemoteControlInput> list = new List<RemoteControlInput>();
    public void Add(RemoteControlInput input)
    {
        list.Add(input);
    }
    public void UpdateParam(string updateMsg)
    {
        RemoteControlInput input = JsonUtility.FromJson<RemoteControlInput>(updateMsg);
        foreach (RemoteControlInput param in list)
        {
            if (param.name == input.name &&
                param.type == input.type)
            {
                // update the value of the parameter
                param.Update(updateMsg);
                break;
            }
        }
    }
    public void FireChanges()
    {
        foreach (RemoteControlInput param in list)
        {
            param.FireChange();
        }
    }
    public void LoadValues(RemoteControlParams loadedParams)
    {
        foreach (RemoteControlInput loadedParam in loadedParams.list)
        {
            foreach (RemoteControlInput param in list)
            {
                if (param.name == loadedParam.name &&
                    param.type == loadedParam.type)
                {
                    // update the value of the parameter
                    param.Update(loadedParam);
                    break;
                }
            }
        }
    }
}
#endif

[System.Serializable]
public class RemoteControlInput
{
    public string name;
    public string type;
    public bool changed = false;
    public bool boolVal;
    public double doubleVal;
    public string stringVal;
    public string colorVal;

    public virtual void Update(string msg) { }
    public virtual void Update(RemoteControlInput newParam) { }
    public virtual void FireChange() { }
}

[System.Serializable]
public class RemoteControlInputColor : RemoteControlInput
{
    public delegate void ChangeHandler(Color c);
    private event ChangeHandler onChangeEvent;

    public RemoteControlInputColor(string name, Color c, ChangeHandler onchange)
    {
        this.type = "color";
        this.name = name;
        this.colorVal = ColorToHex(c);
        this.onChangeEvent = onchange;
    }

    public override void Update(string msg)
    {
        RemoteControlInputColor newval = JsonUtility.FromJson<RemoteControlInputColor>(msg);
        this.colorVal = newval.colorVal;
        changed = true;
    }

    public override void Update(RemoteControlInput newParam)
    {
        this.colorVal = newParam.colorVal;
        changed = true;
    }

    public override void FireChange()
    {
        if (!changed)
        {
            return;
        }

        onChangeEvent(HexToColor(colorVal));
        changed = false;
    }

    string ColorToHex(Color32 color)
    {
        string hex = "#" + color.a.ToString("X2") + color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
        return hex;
    }

    Color HexToColor(string hex)
    {
        byte a = byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
        byte r = byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(7, 2), System.Globalization.NumberStyles.HexNumber);
        return new Color32((byte)r, (byte)g, (byte)b, (byte)a);
    }
}

[System.Serializable]
public class RemoteControlInputBool : RemoteControlInput
{
    public delegate void ChangeHandler(bool b);
    private event ChangeHandler onChangeEvent;

    public RemoteControlInputBool(string name, bool b, ChangeHandler onchange)
    {
        this.type = "bool";
        this.name = name;
        this.boolVal = b;
        this.onChangeEvent = onchange;
    }

    public override void Update(string msg)
    {
        RemoteControlInputBool newval = JsonUtility.FromJson<RemoteControlInputBool>(msg);
        this.boolVal = newval.boolVal;
        changed = true;
    }

    public override void Update(RemoteControlInput newParam)
    {
        this.boolVal = newParam.boolVal;
        changed = true;
    }

    public override void FireChange()
    {
        if (!changed)
        {
            return;
        }

        onChangeEvent(boolVal);
        changed = false;
    }
}

[System.Serializable]
public class RemoteControlInputString : RemoteControlInput
{
    public delegate void ChangeHandler(string text);
    private event ChangeHandler onChangeEvent;

    public RemoteControlInputString(string name, string str, ChangeHandler onchange)
    {
        this.type = "string";
        this.name = name;
        this.stringVal = str;
        this.onChangeEvent = onchange;
    }

    public override void Update(string msg)
    {
        RemoteControlInputString newval = JsonUtility.FromJson<RemoteControlInputString>(msg);
        this.stringVal = newval.stringVal;
        changed = true;
    }

    public override void Update(RemoteControlInput newParam)
    {
        this.stringVal = newParam.stringVal;
        changed = true;
    }

    public override void FireChange()
    {
        if (!changed)
        {
            return;
        }

        onChangeEvent(stringVal);
        changed = false;
    }
}

[System.Serializable]
public class RemoteControlInputDouble : RemoteControlInput
{
    public double min;
    public double max;
    public double step;
    public delegate void ChangeHandler(double d);
    private event ChangeHandler onChangeEvent;

    public RemoteControlInputDouble(string name, double d, ChangeHandler onchange, double min = 0, double max = 100, double step = 1)
    {
        this.type = "double";
        this.name = name;
        this.doubleVal = d;
        this.min = min;
        this.max = max;
        this.step = step;
        this.onChangeEvent = onchange;
    }

    public override void Update(string msg)
    {
        RemoteControlInputDouble newval = JsonUtility.FromJson<RemoteControlInputDouble>(msg);
        this.doubleVal = newval.doubleVal;
        changed = true;
    }

    public override void Update(RemoteControlInput newParam)
    {
        this.doubleVal = newParam.doubleVal;
        changed = true;
    }

    public override void FireChange()
    {
        if (!changed)
        {
            return;
        }

        onChangeEvent(doubleVal);
        changed = false;
    }
}

#if UNITY_UWP
public class RemoteControlUtils
{
    public static string LoadPreset(string name)
    {
        Task<string> t = LoadPresetAsync(name);
        return t.Result;
    }

    private static async Task<string> LoadPresetAsync(string name)
    {
        try
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            folder = await folder.CreateFolderAsync("RemoteControl_presets", CreationCollisionOption.OpenIfExists);
            StorageFile textFile = await folder.GetFileAsync(name);
            return await FileIO.ReadTextAsync(textFile);
        }
        catch (Exception e)
        {
            Debug.Log("exception while saving text file: " + e);
            return "";
        }
    }

    public static void SavePreset(string name, string data)
    {
        SavePresetAsync(name, data);
    }

    private static async void SavePresetAsync(string name, string data)
    {
        try
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            folder = await folder.CreateFolderAsync("RemoteControl_presets", CreationCollisionOption.OpenIfExists);
            StorageFile textFile = await folder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(textFile, data);
        }
        catch (Exception e)
        {
            Debug.Log("exception while saving text file: " + e);
        }
    }

    public static bool DeletePreset(string name)
    {
        Task<bool> t = DeletePresetAsync(name);
        return t.Result;
    }

    public static async Task<bool> DeletePresetAsync(string name)
    {
        try
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            folder = await folder.CreateFolderAsync("RemoteControl_presets", CreationCollisionOption.OpenIfExists);
            StorageFile file = await folder.GetFileAsync(name);
            await file.DeleteAsync(StorageDeleteOption.Default);
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("exception while deleting file: " + e);
            return false;
        }
    }

    public static List<string> GetPresetList()
    {
        Task<List<string>> res = GetPresetListAsync();
        return res.Result;
    }

    public static async Task<List<string>> GetPresetListAsync()
    {
        List<string> files = new List<string>();

        StorageFolder folder = ApplicationData.Current.LocalFolder;
        folder = await folder.CreateFolderAsync("RemoteControl_presets", CreationCollisionOption.OpenIfExists);
        IReadOnlyList<IStorageItem> itemsList = await folder.GetItemsAsync();
        foreach (IStorageItem item in itemsList)
        {
            files.Add(item.Name);
        }

        return files;
    }

    public static string GetLocalIp()
    {
        foreach (HostName localHostName in NetworkInformation.GetHostNames())
        {
            if (localHostName.IPInformation != null)
            {
                if (localHostName.Type == HostNameType.Ipv4)
                {
                    return localHostName.ToString();
                }
            }
        }
        return "0.0.0.0";
    }
}
#endif

[System.Serializable]
public class PresetList
{
    public List<string> list;
    public PresetList(List<string> list)
    {
        this.list = list;
    }
}