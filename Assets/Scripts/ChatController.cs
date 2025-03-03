using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System; // 引入 System 命名空间
using System.Net;
using System.Collections.Generic;

public class AiAnimationController : MonoBehaviour
{
    private Animator animator; // 将你的角色 Animator 组件拖拽到这里

    private int port = 8999;
    private HttpListener listener;
    private bool isRunning = false;

    private int previousInput = 0; // 保存上一次的输入值

    // 定义 emotion 到 animation 的映射
    private Dictionary<string, string> emotionMap = new Dictionary<string, string>()
    {
        { "happy", "haoqi" },      //haoqi 6
        { "sad", "linghun" },     //linghun 5
        { "angry", "yaotou" },     //yaotou 3
        { "sleepy", "keshui" },    //keshui 1

        { "admiration", "haoqi" },     //好奇，可以表达赞赏   haoqi 6
        { "amusement", "haoqi" },      //好笑，也可以用haoqi表现 haoqi 6
        { "anger", "yaotou" },         //摇头，表达愤怒 yaotou 3
        { "annoyance", "yaotou" },      //烦恼，摇头也比较合适 yaotou 3
        { "approval", "haoqi" },      //赞同，好奇的点头可以表示   haoqi 6
        { "caring", "zhentou" },       //枕头，轻微的关心？感觉不是很贴切, 替换为空闲  idle1 7
        { "confusion", "linghun" },    //灵魂出窍，表示困惑  linghun 5
        { "curiosity", "haoqi" },     //好奇，没问题  haoqi 6
        { "desire", "haoqi" },      //渴望，也可以用haoqi表现 haoqi 6
        { "disappointment", "linghun" }, //失望，灵魂出窍  linghun 5
        { "disapproval", "yaotou" },    //不赞同，摇头  yaotou 3
        { "disgust", "yaotou" },       //厌恶，摇头表示  yaotou 3
        { "embarrassment", "keshui" },  //尴尬，类似想睡觉，低头  keshui 1
        { "excitement", "haoqi" },     //兴奋，好奇的表现  haoqi 6
        { "fear", "linghun" },        //恐惧，灵魂出窍  linghun 5
        { "gratitude", "haoqi" },      //感激，好奇的点头  haoqi 6
        { "grief", "linghun" },        //悲痛，灵魂出窍  linghun 5
        { "joy", "haoqi" },           //快乐，好奇心  haoqi 6
        { "love", "zhentou" },        //爱，比较接近？ 替换为空闲  idle2 8
        { "nervousness", "keshui" },   //紧张，类似想睡觉，搓手  keshui 1
        { "optimism", "haoqi" },       //乐观，好奇  haoqi 6
        { "pride", "qizi" },          //旗帜，昂首挺胸？  qizi 4
        { "realization", "linghun" },  //意识到，灵魂出窍  linghun 5
        { "relief", "keshui" },        //安心，放松，想睡觉  keshui 1
        { "remorse", "linghun" },      //懊悔，类似悲伤   linghun 5
        { "sadness", "linghun" },      //悲伤，灵魂出窍  linghun 5
        { "surprise", "haoqi" },       //惊讶，好奇  haoqi 6
        { "neutral", "idle1" }         //中立，站立不动 idle1 7
    };

    private Dictionary<string, int> animationMap = new Dictionary<string, int>()
    {
        { "keshui", 1 },
        { "zhentou", 2 },
        { "yaotou", 3 },
        { "qizi", 4 },
        { "linghun", 5 },
        { "haoqi", 6 },
        { "idle1", 7 },
        { "idle2", 8 },
        { "idle3", 9 }
    };

    [Serializable]
    private class AiCommand
    {
        public string emotion;  // AI 传入的情绪
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component is not assigned. Please assign it in the Inspector.");
            enabled = false; // 禁用脚本
            return;
        }
        StartCoroutine(StartServer());
    }

    IEnumerator StartServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:" + port + "/");
        listener.Start();
        isRunning = true;
        Debug.Log("Server started on port " + port);

        while (isRunning)
        {
            var result = listener.BeginGetContext(ListenerCallback, listener);
            yield return new WaitAsyncResult(result);
        }

        listener.Close();
        Debug.Log("Server stopped");
    }

    void ListenerCallback(IAsyncResult result)
    {
        HttpListener listener = (HttpListener)result.AsyncState;
        HttpListenerContext context = listener.EndGetContext(result);
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        Debug.Log("Request received: " + request.RawUrl);

        // 从请求中读取 JSON 数据
        string requestBody = "";
        using (System.IO.Stream body = request.InputStream)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }
        }

        // 处理 JSON 数据
        try
        {
            AiCommand command = JsonUtility.FromJson<AiCommand>(requestBody);  // 使用 Unity 的 JsonUtility

            // 根据 emotion 查找动画名称
            if (emotionMap.ContainsKey(command.emotion))
            {
                string animationName = emotionMap[command.emotion];
                HandleAnimationCommand(animationName);
            }
            else
            {
                Debug.LogWarning("Unknown emotion: " + command.emotion);
                SendErrorResponse(response, "Unknown emotion: " + command.emotion); //发送错误响应
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing JSON: " + e.Message);
            SendErrorResponse(response, "Invalid JSON format.");
            return; // 确保返回，不再执行后续代码
        }

        // 发送成功响应
        SendSuccessResponse(response);
    }

    void HandleAnimationCommand(string animationName)
    {
        Debug.Log("Received animation command: " + animationName);

        // 检查是否是已知的动画名称
        if (animationMap.ContainsKey(animationName))
        {
            int input = animationMap[animationName]; // 从 Dictionary 获取 Input 值

            // 只在 Input 值发生变化时才设置 Animator 参数
            if (input != previousInput)
            {
                animator.SetInteger("Input", input);
                previousInput = input; // 更新上一次的输入值
            }
            else
            {
                Debug.Log("Input value already set. No need to update Animator.");
            }
        }
        else
        {
            Debug.LogWarning("Unknown animation command: " + animationName);
            //可以选择发送错误回复
        }
    }

    void SendSuccessResponse(HttpListenerResponse response)
    {
        string responseString = "Animation command received and processed.";
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.StatusCode = (int)HttpStatusCode.OK;  // 设置 HTTP 状态码为 200 (OK)
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    void SendErrorResponse(HttpListenerResponse response, string errorMessage)
    {
        string responseString = "Error: " + errorMessage;
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.StatusCode = (int)HttpStatusCode.BadRequest;  // 设置 HTTP 状态码为 400 (BadRequest)
        System.IO.Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }


    void OnDestroy()
    {
        isRunning = false;
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            listener.Close();
        }
    }

    public class WaitAsyncResult : CustomYieldInstruction
    {
        private IAsyncResult asyncResult;

        public WaitAsyncResult(IAsyncResult asyncResult)
        {
            this.asyncResult = asyncResult;
        }

        public override bool keepWaiting
        {
            get { return !asyncResult.IsCompleted; }
        }
    }
}
