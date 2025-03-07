﻿using System;
using System.IO;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
public class HotUpdate : MonoBehaviour
{
    int DownloadCount;
    LoadingUI Loadingui;
    GameObject Loadingobj;
    byte[] ReadPathFildListData;
    byte[] ServerFileListData;

    internal class DownFileInfo
    {
        public string url;
        public string fileName; //相对路径
        public DownloadHandler fildData;
    }

    void Start()
    {
        GameObject go = Resources.Load<GameObject>("LoadingUI");
        Loadingobj = Instantiate(go);
        Loadingobj.transform.SetParent(this.transform);
        Loadingui = Loadingobj.GetComponent<LoadingUI>();
        if (IsFirstInstall())
            ReleaseResoures();
        else
            CheckUpdate();
    }

    bool IsFirstInstall()
    {
        bool isExistsReadPath = FileUtil.IsExists(Path.Combine(PathUtil.ReadPath, AppConst.FileListName));  //判断只读目录是否存在版本文件
        bool isExistsReadWritePath = FileUtil.IsExists(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName));    //判断可读写目录是否存在版本文件
        return isExistsReadPath && !isExistsReadWritePath;
    }

    #region 释放资源
    void ReleaseResoures()
    {
        string url = Path.Combine(PathUtil.ReadPath, AppConst.FileListName);
        DownFileInfo info = new DownFileInfo();
        info.url = url;
        StartCoroutine(DownLoadFild(info, OnDownLoadReadPathFileListActoin));
    }

    void OnDownLoadReadPathFileListActoin(DownFileInfo file)
    {
        ReadPathFildListData = file.fildData.data;
        List<DownFileInfo> fileInfos = GetFildList(file.fildData.text, PathUtil.ReadPath);  //解析文件信息
        StartCoroutine(DownLoadFild(fileInfos, OnReleaseFileActoin, OnReleaseAllFileActoin));  //下载多文件
        Loadingui.InitProgress(100, "正在释放资源 ~ 不消耗流量哦 ~");
    }

    void OnReleaseFileActoin(DownFileInfo file)
    {
        Debug.Log("OnReleaseFileActoin->"+file.fileName);
        string writeFile = Path.Combine(PathUtil.ReadWritePath, file.fileName);
        FileUtil.WriteFile(writeFile, file.fildData.data);
    }

    void OnReleaseAllFileActoin()
    {
        FileUtil.WriteFile(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName), ReadPathFildListData);
        CheckUpdate();
    }
    #endregion
    
    #region 检测更新
    void CheckUpdate()
    {
        DownloadCount = 0;
        string url = Path.Combine(AppConst.ResouresUrl, AppConst.FileListName);
        DownFileInfo info = new DownFileInfo();
        info.url = url;
        StartCoroutine(DownLoadFild(info, OnDownLoadServerFileListActoin));
    }

    void OnDownLoadServerFileListActoin(DownFileInfo file)
    {
        DownloadCount = 0;
        ServerFileListData = file.fildData.data;
        List<DownFileInfo> fileInfos = GetFildList(file.fildData.text, AppConst.ResouresUrl) ;   //获取资源服务器文件信息
        List<DownFileInfo> DownListFiles = new List<DownFileInfo>(); //下载文件集合
        for (int i = 0; i < fileInfos.Count; i++)
        {
            string localFile = Path.Combine(PathUtil.ReadWritePath, fileInfos[i].fileName);
            if (!FileUtil.IsExists(localFile))
            {
                fileInfos[i].url = Path.Combine(AppConst.ResouresUrl, fileInfos[i].fileName);
                DownListFiles.Add(fileInfos[i]);
            }
        }

        if (DownListFiles.Count > 0)
        {
            StartCoroutine(DownLoadFild(fileInfos, OnUpdateFileActoin, OnUpdateAllFileActoin));
            Loadingui.InitProgress(DownListFiles.Count, "正在从资源服务器下载文件中,请稍等.....");
        }
        else
            EnterGame();
    }

    void OnUpdateFileActoin(DownFileInfo file)
    {
        Debug.Log("OnUpdateFileActoin->"+file.url);
        string writeFile = Path.Combine(PathUtil.ReadWritePath, file.fileName);
        FileUtil.WriteFile(writeFile, file.fildData.data);
        DownloadCount++;
        Loadingui.UpdateProgress(DownloadCount);
    }

    void OnUpdateAllFileActoin()
    {
        FileUtil.WriteFile(Path.Combine(PathUtil.ReadWritePath, AppConst.FileListName), ServerFileListData);
        EnterGame();
        Loadingui.InitProgress(0, "正在资源载入中.....");
    }
    #endregion
    
    #region 进入游戏
    void EnterGame()
    {
        GameManager.Instance.GetManager<MessageManager>(GameManager.ManagerName.Message).NotifyMessage(MessageType.GameInit);
        Destroy(Loadingobj);
    }
    #endregion
    
    #region 文件处理
    /// <summary>
    /// 单个文件下载
    /// </summary>
    /// <param name="info">文件信息</param>
    /// <param name="action">回调</param>
    /// <returns></returns>
    IEnumerator DownLoadFild(DownFileInfo info,Action<DownFileInfo> action)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(info.url);
        yield return webRequest.SendWebRequest();
        if (webRequest.isHttpError || webRequest.isNetworkError)
        {
            Debug.Log("Download Fild Error->" + info.url);
            yield break;
        }
        yield return new WaitForSeconds(0.2f);
        info.fildData = webRequest.downloadHandler;
        action?.Invoke(info);
        webRequest.Dispose(); //释放
    }

    /// <summary>
    /// 多个文件下载
    /// </summary>
    /// <param name="infos">文件信息列表</param>
    /// <param name="action">单个下载回调</param>
    /// <param name="downLoadAllAction">结束下载回调</param>
    /// <returns></returns>
    IEnumerator DownLoadFild(List<DownFileInfo> infos, Action<DownFileInfo> action,Action downLoadAllAction)
    {
        if (infos != null)
        {
            var en = infos.GetEnumerator();
            while (en.MoveNext())
            {
                yield return DownLoadFild(en.Current, action);
            }
        }
        else
            Debug.Log("DownLoadFild Exception!!!");
        downLoadAllAction?.Invoke();
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    List<DownFileInfo> GetFildList(string fileData,string path)
    {
        string content = fileData.Trim().Replace("\r", "");  //去除符号
        string[] files = content.Split('\n'); //切割每一行
        List<DownFileInfo> downFileInfos = new List<DownFileInfo>(files.Length);
        for (int i = 0; i < files.Length; i++)
        {
            string [] info = files[i].Split('|'); //切割信息
            DownFileInfo fileInfo = new DownFileInfo();
            fileInfo.fileName = info[1];
            fileInfo.url = Path.Combine(path, info[1]);
            downFileInfos.Add(fileInfo);
        }
        return downFileInfos;
    }
    #endregion
}
