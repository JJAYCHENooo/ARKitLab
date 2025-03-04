﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using UnityEngine.Networking;

public class ModelController : NetworkBehaviour {

    public UnityARCameraManager UnityARCameraManager;
    public GameObject Model;
    public GameObject Button;
    public GameObject GeneratePlane, FocusSquare;
    [HideInInspector]
    public Vector3 transforme; // remove vector
    public GameObject obj1, obj2, obj3;
    [HideInInspector]
    public float distance1,distance2,distance;
    [HideInInspector]
    public Vector3 oldRemove1, oldRemove2;
    public GameObject ShowButton;

    private bool flag;
    private bool adjustable = true;

    /* JJAYCHEN */
    public HintController HintController;
    private float time = 0.0f;
    public float thresholdTime;
    private bool hintFinished = false;
    /* == JJAYCHEN */

    public delegate void SetModelActiveHandler();
    public event SetModelActiveHandler SetModelActiveEvent;
    public delegate void SetModelInactiveHandler();
    public event SetModelInactiveHandler SetModelInactiveEvent;
    public delegate void ResetHandler();
    public event ResetHandler ResetHandlerEvent;

    Vector3 oldPosition1;
    Vector3 oldPosition2;

    // Use this for initialization
    void Start () {
        ResetHandlerEvent += SetModelInactive;
        ResetHandlerEvent += HideButton;
        ResetHandlerEvent += SetAdjustableTrue;
    }
	
	// Update is called once per frame
	void Update () {
        //CheckButton();
        //Debug.Log("模型ActiveFalse");
        if (Model.activeSelf == false)
        {
            if (Input.GetMouseButtonDown(0))
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    //Debug.Log("执行HitTest");
                    Vector3 screenPos = Camera.main.ScreenToViewportPoint(touch.position);
                    ARPoint point = new ARPoint
                    {
                        x = screenPos.x,
                        y = screenPos.y
                    };
                    List<ARHitTestResult> hitTestResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, ARHitTestResultType.ARHitTestResultTypeExistingPlane);
                    if (flag == false && hitTestResults.Count > 0)
                    {
                        UnityARCameraManager.CloseDetection();
                        flag = true;
                        GeneratePlane.GetComponent<FocusSquare>().enabled = false;
                        FocusSquare.SetActive(false);
                        HintController.ShowElement(3);
                    }
                    /* == 第一次点击的时候关闭平面检测 */

                    //Debug.Log("放置模型");

                    /* UNET */
                    var player = ClientScene.localPlayers[0].gameObject.GetComponent<Player>();
                    player.CheckAuthority(GetComponent<NetworkIdentity>(), player.GetComponent<NetworkIdentity>());
                    CmdChangeTransform(UnityARMatrixOps.GetPosition(hitTestResults[hitTestResults.Count - 1].worldTransform), UnityARMatrixOps.GetRotation(hitTestResults[hitTestResults.Count - 1].worldTransform));
                    CmdSetModelActive();
                    CmdSetConfirmButtonActive();
                    /* == UNET */

                    //Button.SetActive(true);
                }
            }
        }else if(adjustable)
        {
            if (Input.touchCount == 2)
            {
                var touch1 = Input.GetTouch(0);
                var touch2 = Input.GetTouch(1);
                if(touch1.phase == TouchPhase.Moved && touch2.phase == TouchPhase.Moved)
                {
                    Vector3 screenPos = Camera.main.ScreenToViewportPoint((touch1.position+touch2.position)/2);
                    ARPoint point = new ARPoint
                    {
                        x = screenPos.x,
                        y = screenPos.y
                    };
                    List<ARHitTestResult> hitTestResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, ARHitTestResultType.ARHitTestResultTypeExistingPlane);
                    //Model.transform.position = UnityARMatrixOps.GetPosition(hitTestResults[hitTestResults.Count - 1].worldTransform);

                    /* UNET */
                    var player = ClientScene.localPlayers[0].gameObject.GetComponent<Player>();
                    player.CheckAuthority(GetComponent<NetworkIdentity>(), player.GetComponent<NetworkIdentity>());
                    CmdChangeTransform(UnityARMatrixOps.GetPosition(hitTestResults[hitTestResults.Count - 1].worldTransform), Model.transform.rotation);
                    /* == UNET */
                }

                //保证重新开始双指触摸记录的初始位置不是松开手时的位置，而是初始触摸的位置
                if (Input.GetTouch(0).phase == TouchPhase.Began || Input.GetTouch(1).phase == TouchPhase.Began)
                {
                    //记录初始位置
                    oldPosition1 = Input.GetTouch(0).position;
                    oldPosition2 = Input.GetTouch(1).position;
                }

                if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
                {
                    //计算出当前两点触摸点的位置
                    var tempPosition1 = Input.GetTouch(0).position;
                    var tempPosition2 = Input.GetTouch(1).position;

                    float currentTouchDistance = Vector2.Distance(tempPosition1, tempPosition2);
                    float lastTouchDistance = Vector2.Distance(oldPosition1, oldPosition2);

                    //计算上次和这次双指触摸之间的距离差距
                    //然后去更改摄像机的距离
                    float distance = currentTouchDistance - lastTouchDistance;
                    float scaleFactor = distance / 200f;
                    Vector3 localScale = Model.transform.localScale;
                    Vector3 scale = new Vector3(localScale.x + scaleFactor,
                        localScale.y + scaleFactor,
                        localScale.z + scaleFactor);
                    //在什么情况下进行缩放
                    if (scale.x >= 0.2f && scale.y >= 0.2f && scale.z >= 0.2f)
                    {
                        //Model.transform.localScale = scale;
                        var player = ClientScene.localPlayers[0].gameObject.GetComponent<Player>();
                        player.CheckAuthority(GetComponent<NetworkIdentity>(), player.GetComponent<NetworkIdentity>());
                        CmdChangeScale(scale);
                    }
                    //备份上一次触摸点的位置，用于对比
                    //也是松开手时的位置
                    oldPosition1 = tempPosition1;
                    oldPosition2 = tempPosition2;
                }

                //if (Vector2.Dot(touch1.deltaPosition,touch2.deltaPosition)<=0 && touch1.position.y < touch2.position.y)
                //{
                //    Model.transform.Rotate(Vector3.down * touch1.deltaPosition.x * 0.75f, Space.World);
                //}
                //else if (Vector2.Dot(touch1.deltaPosition, touch2.deltaPosition) <= 0 && touch1.position.y > touch2.position.y)
                //{
                //    Model.transform.Rotate(Vector3.down * touch2.deltaPosition.x * 0.75f, Space.World);
                //}
            }
            else if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                Vector2 deltaPos = Input.GetTouch(0).deltaPosition;
                //Model.transform.Rotate(Vector3.down * deltaPos.x * 0.5f, Space.World);
                var player = ClientScene.localPlayers[0].gameObject.GetComponent<Player>();
                player.CheckAuthority(GetComponent<NetworkIdentity>(), player.GetComponent<NetworkIdentity>());
                CmdRotate(deltaPos);
            }
        }
        if (hintFinished)
        {
            time += Time.deltaTime;
            if (time > thresholdTime)
            {
                HintController.HideElement();
                hintFinished = false;// not really unfinished
            }
        }
    }

    void SetModelInactive()
    {
        Model.SetActive(false);
        if (SetModelInactiveEvent != null) SetModelInactiveEvent();
    }

    public void  Reset()
    {
        //if (ResetHandlerEvent != null) ResetHandlerEvent();
        var player = ClientScene.localPlayers[0].gameObject.GetComponent<Player>();
        player.CheckAuthority(GetComponent<NetworkIdentity>(), player.GetComponent<NetworkIdentity>());
        CmdReset();
    }

    public void Confirm()
    {
        CmdConfirm();
    }

    private bool first = true;
    public void SetAdjustableFalse()
    {
        adjustable = false;
            transforme = obj1.transform.localPosition - obj2.transform.localPosition;
            oldRemove1 = obj1.transform.position;
            oldRemove2 = obj2.transform.position;
            distance1 = Vector3.Distance(obj1.transform.position, obj3.transform.position);
            distance2 = Vector3.Distance(obj2.transform.position, obj3.transform.position);

    }

    public void SetAdjustableTrue()
    {
        adjustable = true;
    }

    public void HideButton()
    {
        Button.SetActive(false);
    }
    public bool CheckButton()
    {
        if (!adjustable) return true;
        return false;
    }

    /* UNET */

    [Command]
    public void CmdChangeTransform(Vector3 position, Quaternion rotation)
    {
        RpcChangeTransform(position, rotation);
    }

    [ClientRpc]
    public void RpcChangeTransform(Vector3 position, Quaternion rotation)
    {
        Model.transform.position = position;
        Model.transform.rotation = rotation;
    }

    [Command]
    public void CmdChangeScale(Vector3 scale)
    {
        RpcChangeScale(scale);
    }

    [ClientRpc]
    public void RpcChangeScale(Vector3 scale)
    {
        Model.transform.localScale = scale;
    }

    [Command]
    public void CmdRotate(Vector3 deltaPos)
    {
        RpcRotate(deltaPos);
    }

    [ClientRpc]
    public void RpcRotate(Vector3 deltaPos)
    {
        Model.transform.Rotate(Vector3.down * deltaPos.x * 0.5f, Space.World);
    }

    [Command]
    void CmdSetModelActive()
    {
        RpcSetModelActive();
    }

    [ClientRpc]
    void RpcSetModelActive()
    {
        Model.SetActive(true);
        if (SetModelActiveEvent != null) SetModelActiveEvent();
    }

    [Command]
    void CmdSetConfirmButtonActive()
    {
        RpcSetConfirmButtonActive();
    }

    [ClientRpc]
    void RpcSetConfirmButtonActive()
    {
        Button.SetActive(true);
    }

    [Command]
    void CmdConfirm()
    {
        RpcConfirm();
    }

    [ClientRpc]
    void RpcConfirm()
    {
        SetAdjustableFalse();
        HideButton();
        HintController.ShowElement(4);
        hintFinished = true;
        ShowButton.SetActive(true);
    }

    [Command]
    public void CmdReset()
    {
        RpcReset();
    }

    [ClientRpc]
    public void RpcReset()
    {
        if (ResetHandlerEvent != null) ResetHandlerEvent();
    }
}
