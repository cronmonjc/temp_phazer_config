using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI Component.  Handles display of a specific section of the BOM, as defined by a value from <see cref="BOMControl.BOMType"/>.
/// </summary>
public class BOMControl : MonoBehaviour {

    /// <summary>
    /// Describes the type of BOM section in use.
    /// </summary>
    public enum BOMType {
        Lights, Lenses, Flashers
    }

    /// <summary>
    /// Is this controlling the lights?
    /// </summary>
    public BOMType type;

    /// <summary>
    /// The elements tracked by this control.  Key is the part number, value is the element.
    /// </summary>
    private Dictionary<string, BOMElement> elements;

    /// <summary>
    /// A Dictionary holding the counts of each part number
    /// </summary>
    private Dictionary<string, int> counts;
    /// <summary>
    /// A Dictionary holding a light head description for each part number
    /// </summary>
    private Dictionary<string, LightHead> descsHead;
    /// <summary>
    /// A Dictionary holding a lens description for each part number
    /// </summary>
    private Dictionary<string, BarSegment> descsLens;

    /// <summary>
    /// The "unconfigured" element.  Set via Unity Inspector.
    /// </summary>
    public BOMElement unconfigured;

    /// <summary>
    /// Prefab of an element.  Set via Unity Inspector.
    /// </summary>
    public GameObject elementPrefab;

    /// <summary>
    /// A List of the part numbers in use.
    /// </summary>
    private List<string> parts;

    /// <summary>
    /// Awake is called once, immediately as the object is created (typically at load time)
    /// </summary>
    void Awake() {
        elements = new Dictionary<string, BOMElement>();
        counts = new Dictionary<string, int>();
        if(type == BOMType.Lights) {
            descsHead = new Dictionary<string, LightHead>();
        } else if(type == BOMType.Lenses) {
            descsLens = new Dictionary<string, BarSegment>();
        }
        parts = new List<string>();
    }

    /// <summary>
    /// Update is called once each frame
    /// </summary>
    void Update() {
        parts.Clear(); // Clear out the part number list
        foreach(string alpha in new List<string>(counts.Keys)) {
            counts[alpha] = 0; // Clear out the counts
        }

        if(type == BOMType.Lights) {
            foreach(string alpha in new List<string>(descsHead.Keys)) {
                descsHead[alpha] = null;
            }
            int unconfig = 0;
            foreach(LightHead lh in BarManager.inst.allHeads) {
                if(lh.gameObject.activeInHierarchy) {
                    if(lh.lhd.style != null) {
                        string part = lh.PartNumber;
                        if(parts.Contains(part)) {
                            counts[part]++;
                        } else {
                            counts[part] = 1;
                            descsHead[part] = lh;
                            parts.Add(part);
                        }
                    } else {
                        unconfig++;
                    }
                }
            }

            unconfigured.quantity = unconfig;
            unconfigured.gameObject.SetActive(unconfig > 0);

            foreach(string alpha in parts) {
                if(elements.ContainsKey(alpha)) { // If the BOM Element already exists, recycle
                    elements[alpha].quantity = counts[alpha];
                    elements[alpha].headToDescribe = descsHead[alpha];
                } else { // Otherwise make a new one
                    BOMElement newbie = AddItem(alpha);
                    newbie.type = type;
                    newbie.headToDescribe = descsHead[alpha];
                    newbie.quantity = counts[alpha];
                }
            }
            foreach(string alpha in new List<string>(elements.Keys)) {
                if(!parts.Contains(alpha)) {
                    RemoveItem(alpha);
                }
            }
        } else if(type == BOMType.Lenses) {
            foreach(string alpha in new List<string>(descsLens.Keys)) {
                descsLens[alpha] = null;
            }
            int unconfig = 0;
            foreach(BarSegment seg in BarManager.inst.allSegs) {
                if(seg.Visible) {
                    if(seg.lens != null) {
                        string part = seg.LensPart;
                        if(parts.Contains(part)) {
                            counts[part]++;
                        } else {
                            counts[part] = 1;
                            descsLens[part] = seg;
                            parts.Add(part);
                        }
                    } else {
                        unconfig++;
                    }
                }
            }

            unconfigured.quantity = unconfig;
            unconfigured.gameObject.SetActive(unconfig > 0);

            foreach(string alpha in parts) {
                if(elements.ContainsKey(alpha)) {
                    elements[alpha].quantity = counts[alpha];
                    elements[alpha].lensToDescribe = descsLens[alpha];
                } else {
                    BOMElement newbie = AddItem(alpha);
                    newbie.type = type;
                    newbie.lensToDescribe = descsLens[alpha];
                    newbie.quantity = counts[alpha];
                }
            }
            foreach(string alpha in new List<string>(elements.Keys)) {
                if(!counts.ContainsKey(alpha) || counts[alpha] == 0) {
                    RemoveItem(alpha);
                }
            }
        //} else if(type == BOMType.FlasherBundles) {
        //    string[] wireClusters = MainCameraControl.GetFlasherBundles();
        //    HashSet<string> uniqueClusters = new HashSet<string>(wireClusters);
        //    foreach(string alpha in wireClusters) {
        //        if(counts.ContainsKey(alpha)) {
        //            counts[alpha]++;
        //        } else {
        //            counts[alpha] = 1;
        //        }
        //    }

        //    foreach(string alpha in uniqueClusters) {
        //        if(elements.ContainsKey(alpha)) {
        //            elements[alpha].quantity = counts[alpha];
        //            elements[alpha].bundleToDescribe = alpha;
        //        } else {
        //            BOMElement newbie = AddItem(alpha);
        //            newbie.type = type;
        //            newbie.bundleToDescribe = alpha;
        //            newbie.quantity = counts[alpha];
        //        }
        //    }
        //    foreach(string alpha in new List<string>(elements.Keys)) {
        //        if(!counts.ContainsKey(alpha)) {
        //            RemoveItem(alpha);
        //        }
        //    }


        }
    }

    /// <summary>
    /// Creates a <see cref="BOMElement"/> <see cref="UnityEngine.GameObject"/> and returns it for more processing.
    /// </summary>
    /// <param name="partNum">The part number of the element being created.</param>
    /// <returns>The new BOMElement.</returns>
    public BOMElement AddItem(string partNum) {
        if(elements.ContainsKey(partNum)) {
            return elements[partNum];
        } else {
            int childIndex = transform.GetSiblingIndex() + 3 + elements.Count; // Always place the new element at the bottom of the section
            GameObject newbie = GameObject.Instantiate(elementPrefab) as GameObject;
            newbie.transform.SetParent(transform.parent, false);
            newbie.transform.SetSiblingIndex(childIndex);
            newbie.transform.localScale = Vector3.one;
            BOMElement newelem = newbie.GetComponent<BOMElement>();
            elements[partNum] = newelem;
            return newelem;
        }
    }

    /// <summary>
    /// Destroys a BOMElement by part number.
    /// </summary>
    /// <param name="partNum">The part number of the element to be destroyed.</param>
    public void RemoveItem(string partNum) {
        if(elements.ContainsKey(partNum)) {
            Destroy(elements[partNum].gameObject);
            elements.Remove(partNum);
        }
    }
}