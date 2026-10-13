using System;
using System.Collections.Generic;

// Field names match panels.json exactly (required by Unity's JsonUtility).

[Serializable]
public class NarrationMeta
{
    public string game;
    public string version;
    public string start_panel;
}

[Serializable]
public class NarrationChoice
{
    public string label;
    public string next;
    public List<string> requires = new List<string>();
}

[Serializable]
public class NarrationPanel
{
    public string id;
    public string type;
    public string location;
    public string speaker;
    public string text;
    public List<string> conditions = new List<string>();
    public List<string> sets = new List<string>();
    public List<NarrationChoice> choices = new List<NarrationChoice>();
    public string next;
    public string notes;
}

[Serializable]
public class NarrationScript
{
    public NarrationMeta meta;
    public List<string> panel_types;
    public List<NarrationPanel> panels;
}