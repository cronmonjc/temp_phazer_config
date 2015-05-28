﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GUI Item.  Disables the Continue Button if there is a single Fatal Issue that occurs.
/// </summary>
public class IssueTrack : MonoBehaviour {
    /// <summary>
    /// List of Fatal Issues.
    /// </summary>
    public IssueChecker[] FatalIssues;
    /// <summary>
    /// The Button to disable if there is a fatal issue.
    /// </summary>
    private Button ContinueButton;
    
    // Update is called once per frame
    void Update() {
        if(ContinueButton == null) { ContinueButton = GetComponent<Button>(); }

        foreach(IssueChecker alpha in FatalIssues) {
            if(alpha.DoCheck()) {
                ContinueButton.interactable = false;
                return;
            }
        }
        ContinueButton.interactable = true;
        return;
    }
}