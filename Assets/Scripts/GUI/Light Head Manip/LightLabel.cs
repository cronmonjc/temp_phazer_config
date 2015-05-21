﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LightLabel : MonoBehaviour {
    public Transform target;
    public Text label, label2;
    public Image background, secondImage, selectionImage;

    private LightHead lh;

    public static CameraControl cam;
    public static bool showParts;
    private StyleNode lastStyle;

    void Start() {
        selectionImage.gameObject.SetActive(false);
        selectionImage.transform.rotation = Quaternion.identity;
        if(lh == null) lh = target.GetComponent<LightHead>();
        if(lh.isSmall) {
            ((RectTransform)transform).sizeDelta = new Vector2(65, 48);
        }
        showParts = false;
        Refresh();
    }

    public void Refresh(bool showHeadNumber = false) {
        string prefix = "";

        if(showHeadNumber) {
            for(int i = 0; i < BarManager.headNumber.Length; i++) {
                if(BarManager.headNumber[i] == lh) {
                    prefix = "(" + (i + 1) + ") ";
                    break;
                }
            }
            if(prefix == "") {
                Debug.LogError("Failed to find a head number for " + transform.GetPath());
                prefix = "(?) ";
            }
        }

        if(showParts) {
            if(lh.lhd.style != null) {
                label2.text = label.text = prefix + lh.PartNumber;
                label2.color = label.color = Color.black;
                secondImage.color = background.color = Color.white;
            }
        } else {
            if(lh.lhd.style != null) {
                label2.text = label.text = prefix + (lh.lhd.optic.styles.Count > 1 ? lh.lhd.style.name + " " : "") + lh.lhd.optic.name;
                Color clr = lh.lhd.style.color;
                background.color = clr;
                if(clr.r + clr.g < clr.b) {
                    label.color = Color.white;
                } else {
                    label.color = Color.black;
                }
                if(lh.lhd.style.isDualColor) {
                    clr = lh.lhd.style.color2;
                }
                if(clr.r + clr.g < clr.b) {
                    label2.color = Color.white;
                } else {
                    label2.color = Color.black;
                }
                secondImage.color = clr;
            } else {
                label2.text = label.text = prefix + "Empty";
                label2.color = label.color = Color.white;
                secondImage.color = background.color = new Color(0, 0, 0, 0.45f);
            }
        }

        lastStyle = lh.lhd.style;
    }

    void Update() {
        if(CameraControl.funcBeingTested != AdvFunction.NONE) return;
        if(cam == null) cam = FindObjectOfType<CameraControl>();
        else {
            if(target != null) {
                transform.position = target.position;
                transform.rotation = target.rotation;

                if(!target.gameObject.activeInHierarchy) {
                    gameObject.SetActive(false);
                }
            }
        }

        if(lh == null) lh = target.GetComponent<LightHead>();
        if(lh.lhd.style != lastStyle) {
            Refresh();
        }

        if(lh.Selected) {
            selectionImage.gameObject.SetActive(true);
            selectionImage.transform.Rotate(new Vector3(0, 0, 20f) * Time.deltaTime);
        } else {
            selectionImage.gameObject.SetActive(false);
            selectionImage.transform.rotation = Quaternion.identity;
        }
    }
}
