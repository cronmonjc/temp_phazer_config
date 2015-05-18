﻿using UnityEngine;
using System.Collections;
using fNbt;
using PdfSharp;
using System.Collections.Generic;

public class BarManager : MonoBehaviour {
    private bool savePDF = false;

    [Range(0,4)]
    public int BarSize = 3;
    public TDOption td;

    public NbtCompound patts;

    public static BarManager inst;
    public List<LightHead> allHeads;

    void Awake() {
        patts = new NbtCompound("pats");

        foreach(string alpha in new string[] { "td", "lall", "rall", "ltai", "rtai", "cru", "cal", "emi", "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw", "dim", "traf" }) {
            patts.Add(new NbtCompound(alpha));
        }

        patts.Get<NbtCompound>("traf").AddRange(new NbtShort[] { new NbtShort("enr1", 0), new NbtShort("enr2", 0) });

        foreach(string alpha in new string[] { "td", "lall", "rall", "ltai", "rtai", "cru", "cal", "emi", "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw", "dim" }) {
            patts.Get<NbtCompound>(alpha).AddRange(new NbtShort[] { new NbtShort("enf1", 0), new NbtShort("enf2", 0), new NbtShort("enr1", 0), new NbtShort("enr2", 0) });
        }

        patts.Get<NbtCompound>("dim").Add(new NbtShort("dimp", 0));

        foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl", "dcw" }) {
            patts.Get<NbtCompound>(alpha).AddRange(new NbtShort[] { new NbtShort("phf1", 0), new NbtShort("phf2", 0), new NbtShort("phr1", 0), new NbtShort("phr2", 0) });
        }

        patts.Get<NbtCompound>("traf").Add(new NbtCompound("patt", new NbtTag[] { new NbtShort("left", 0), new NbtShort("rite", 0), new NbtShort("cntr", 0) }));

        foreach(string alpha in new string[] { "l1", "l2", "l3", "l4", "l5", "tdp", "icl", "afl" }) {
            patts.Get<NbtCompound>(alpha).Add(new NbtCompound("pat1", new NbtTag[] { new NbtShort("fcen", 0), new NbtShort("finb", 0), new NbtShort("foub", 0), new NbtShort("ffar", 0), new NbtShort("fcor", 0),
                                                                                     new NbtShort("rcen", 0), new NbtShort("rinb", 0), new NbtShort("roub", 0), new NbtShort("rfar", 0), new NbtShort("rcor", 0) }));
            patts.Get<NbtCompound>(alpha).Add(new NbtCompound("pat2", new NbtTag[] { new NbtShort("fcen", 0), new NbtShort("finb", 0), new NbtShort("foub", 0), new NbtShort("ffar", 0), new NbtShort("fcor", 0),
                                                                                     new NbtShort("rcen", 0), new NbtShort("rinb", 0), new NbtShort("roub", 0), new NbtShort("rfar", 0), new NbtShort("rcor", 0) }));
        }

        allHeads = new List<LightHead>();
        inst = this;
    }

    void Start() {
        allHeads.AddRange(transform.GetComponentsInChildren<LightHead>(true));
    }

    public static string GetFnString(Transform t, AdvFunction f) {
        switch(f) {
            case AdvFunction.ALLEY:
                if(t.position.x < 0) {
                    return "lall";
                } else {
                    return "rall";
                }
            case AdvFunction.CRUISE:
                return "cru";
            case AdvFunction.DIM:
                return "dim";
            case AdvFunction.EMITTER:
                return "emi";
            case AdvFunction.ICL:
                return "icl";
            case AdvFunction.LEVEL1:
                return "l1";
            case AdvFunction.LEVEL2:
                return "l2";
            case AdvFunction.LEVEL3:
                return "l3";
            case AdvFunction.LEVEL4:
                return "l4";
            case AdvFunction.LEVEL5:
                return "l5";
            case AdvFunction.STT_AND_TAIL:
                if(t.position.x < 0) {
                    return "ltai";
                } else {
                    return "rtai";
                }
            case AdvFunction.TAKEDOWN:
                return "td";
            case AdvFunction.TRAFFIC:
                return "traf";
            case AdvFunction.T13:
                return "cal";
            default:
                return null;
        }
    }

    public void SetBarSize(int to) {
        if(to < 5 && to > -1) {
            BarSize = to;
            foreach(SizeOptionControl soc in GetComponentsInChildren<SizeOptionControl>(true)) {
                soc.ShowLong = true;
            }
            FindObjectOfType<CameraControl>().OnlyCamSelected.Clear();
            foreach(LightLabel ll in GameObject.Find("BarCanvas/Labels").GetComponentsInChildren<LightLabel>(true)) {
                ll.Refresh();
            }
        }
    }

    public void SetTDOption(int to) {
        td = (TDOption)to;
        foreach(LightLabel ll in GameObject.Find("BarCanvas/Labels").GetComponentsInChildren<LightLabel>(true)) {
            ll.Refresh();
        }
    }

    public void Save(string filename) {
        if(savePDF) { SavePDF(filename); return; }
        
        NbtCompound root = new NbtCompound("root");

        root.Add(new NbtByte("size", (byte)BarSize));

        NbtList lightList = new NbtList("lite");
        foreach(LightHead lh in GetComponentsInChildren<LightHead>(true)) {
            NbtCompound lightCmpd = new NbtCompound();
            lightCmpd.Add(new NbtString("path", lh.transform.GetPath()));
            if(lh.lhd.style != null) {
                lightCmpd.Add(new NbtString("optc", lh.lhd.optic.partNumber));
                lightCmpd.Add(new NbtString("styl", lh.lhd.style.name));
            }

            byte fn = 0;
            foreach(BasicFunction bfn in lh.lhd.funcs) {
                fn |= (byte)bfn;
            }
            lightCmpd.Add(new NbtByte("func", fn));

            lightList.Add(lightCmpd);
        }
        root.Add(lightList);

        root.Add(patts);

        NbtList socList = new NbtList("soc");
        foreach(SizeOptionControl soc in transform.GetComponentsInChildren<SizeOptionControl>(true)) {
            NbtCompound socCmpd = new NbtCompound();
            socCmpd.Add(new NbtString("path", soc.transform.GetPath()));
            socCmpd.Add(new NbtByte("isLg", soc.ShowLong ? (byte)1 : (byte)0));
            socList.Add(socCmpd);
        }
        root.Add(socList);

        NbtFile file = new NbtFile(root);
        if(!filename.EndsWith(".bar.nbt")) {
            filename = filename + ".bar.nbt";
        }
        file.SaveToFile(filename, NbtCompression.None);
    }

    public void Open(string filename) {
        NbtFile file = new NbtFile(filename);

        NbtCompound root = file.RootTag;
        BarSize = root["size"].IntValue;

        NbtList lightList = (NbtList)root["lite"];
        NbtList socList = (NbtList)root["soc"];
        Dictionary<string, LightHead> lights = new Dictionary<string, LightHead>();
        Dictionary<string, SizeOptionControl> socs = new Dictionary<string, SizeOptionControl>();

        foreach(LightHead lh in transform.GetComponentsInChildren<LightHead>(true)) {
            lights[lh.transform.GetPath()] = lh;
        }
        foreach(SizeOptionControl soc in transform.GetComponentsInChildren<SizeOptionControl>(true)) {
            socs[soc.transform.GetPath()] = soc;
        }
        foreach(NbtTag alpha in lightList) {
            NbtCompound lightCmpd = alpha as NbtCompound;
            LightHead lh = lights[lightCmpd["path"].StringValue];

            if(lightCmpd.Contains("optc")) {
                LocationNode ln = LightDict.inst.FetchLocation(lh.loc);
                string partNum = lightCmpd["optc"].StringValue;

                foreach(OpticNode on in ln.optics.Values) {
                    if(on.partNumber == partNum) {
                        lh.SetOptic(on.name, BasicFunction.NULL, false);
                        string styleName = lightCmpd["styl"].StringValue;
                        foreach(StyleNode sn in on.styles.Values) {
                            if(sn.name == styleName) {
                                lh.SetStyle(sn);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            byte fn = lightCmpd["func"].ByteValue;
            List<BasicFunction> potential = new List<BasicFunction>();
            potential.Add(BasicFunction.FLASHING);
            switch(lh.loc) {
                case Location.ALLEY:
                    potential.Add(BasicFunction.FLASH_ALLEY);
                    break;
                case Location.FRONT:
                    potential.Add(BasicFunction.FLASH_TAKEDOWN);
                    potential.Add(BasicFunction.EMITTER);
                    potential.Add(BasicFunction.CAL_STEADY);
                    break;
                case Location.REAR:
                    potential.Add(BasicFunction.FLASH_TAKEDOWN);
                    potential.Add(BasicFunction.TRAFFIC);
                    break;
                case Location.FAR_REAR:
                    potential.Add(BasicFunction.FLASH_TAKEDOWN);
                    potential.Add(BasicFunction.STT);
                    break;
                case Location.FRONT_CORNER:
                case Location.REAR_CORNER:
                    potential.Add(BasicFunction.FLASH_TAKEDOWN);
                    potential.Add(BasicFunction.CRUISE);
                    break;
            }
            lh.lhd.funcs.Clear();
            foreach(BasicFunction bfn in potential) {
                if(((byte)bfn & fn) != 0) {
                    lh.lhd.funcs.Add(bfn);
                }
            }
        }

        patts = root.Get<NbtCompound>("pats");

        foreach(NbtTag alpha in socList) {
            NbtCompound socCmpd = alpha as NbtCompound;
            SizeOptionControl soc = socs[socCmpd["path"].StringValue];
            soc.ShowLong = (socCmpd["isLg"].ByteValue == 1);
        }
    }

    public void StartPDF() {
        savePDF = true;
    }

    public void SavePDF(string filename) {
        

        savePDF = false;
    }

    public void Clear() {
        foreach(LightHead lh in transform.GetComponentsInChildren<LightHead>(true)) {
            lh.SetOptic("");
            lh.patterns.Clear();
        }
        foreach(SizeOptionControl soc in transform.GetComponentsInChildren<SizeOptionControl>(true)) {
            soc.ShowLong = true;
        }
    }
}