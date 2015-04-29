﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SplitMerge : MonoBehaviour {
    public static CameraControl cam;

    public bool IsMerge;

    void Update() {
        if(cam == null) cam = FindObjectOfType<CameraControl>();
        foreach(LightHead lh in FindObjectsOfType<LightHead>()) {
            if(!lh.Selected) continue;
            SizeOptionControl soc = null;
            for(Transform t = transform; soc == null && t != null; t = t.parent) {
                soc = t.GetComponent<SizeOptionControl>();
            }
            if(soc == null) {
                continue;
            }

            if((IsMerge && !soc.ShowLong) || (!IsMerge && soc.ShowLong)) {
                GetComponent<Button>().interactable = true;
                return;
            }
        }
        GetComponent<Button>().interactable = false;
    }

    public void Act(bool merge) {
        List<LightHead> temp = new List<LightHead>(FindObjectsOfType<LightHead>());
        foreach(LightHead lh in temp) {
            if(!lh.Selected) continue;
            SizeOptionControl soc = null;
            for(Transform t = transform; soc == null && t != null; t = t.parent) {
                soc = t.GetComponent<SizeOptionControl>();
            }
            if(soc == null) {
                continue;
            }

            soc.ShowLong = merge;
            lh.Selected = false;
            foreach(LightHead alpha in soc.transform.GetComponentsInChildren<LightHead>(false)) {
                if(!alpha.Selected) {
                    alpha.Selected = true;
                    alpha.patterns.Clear();
                    foreach(Function f in lh.patterns.Keys) {
                        if(alpha.CapableFunctions.Contains(f)) {
                            alpha.patterns[f] = lh.patterns[f];
                        }
                    }
                    if(lh.lhd.optic != null) {
                        if(merge) {
                            if(lh.lhd.optic.lgEquivalent.Length > 0) {
                                alpha.SetOptic(lh.lhd.optic.lgEquivalent, false);
                                alpha.SetStyle(lh.lhd.style);
                            }
                        } else {
                            if(lh.lhd.optic.smEquivalent.Length > 0) {
                                alpha.SetOptic(lh.lhd.optic.smEquivalent, false);
                                alpha.SetStyle(lh.lhd.style);
                            }
                        }
                    }
                }
            }
        }
        cam.os.Refresh();
    }
}
