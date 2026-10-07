using System;
using System.Collections;
using Pine;
using Pine.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PineToolkitRuntimeChecks : MonoBehaviour
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        _checks++;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Begin()
    {
        _checks = 0;
        new GameObject("Toolkit runtime checks").AddComponent<PineToolkitRuntimeChecks>();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;
        UIDocument appDocument = null;
        foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            if (document.rootVisualElement?.Q<Label>("Count") != null)
                appDocument = document;
        Check(appDocument != null, "Generated App.Mount starts the selected renderer");
        var counter = appDocument.rootVisualElement.Q<Label>("Count");
        var increment = appDocument.rootVisualElement.Q<Button>("Increment");
        var input = new UnityEngine.Event
        {
            type = EventType.MouseDown,
            button = 0,
            mousePosition = increment.worldBound.center,
        };
        using (var down = PointerDownEvent.GetPooled(input))
            increment.SendEvent(down);
        input.type = EventType.MouseUp;
        using (var up = PointerUpEvent.GetPooled(input))
            increment.SendEvent(up);
        Check(
            counter.text == "Count: 1",
            "Native pointer events invoke a scoped reactive button callback"
        );
        var texture = new RenderTexture(1024, 768, 24);
        texture.Create();
        var paintColor = P.Source(Color.blue);
        var enteredText = P.Source("");
        int paints = 0;
        PineToolkitBehaviourProbe behaviour = null;
        using var mount = P.Mount(
            () =>
                P.VisualElement(
                    style: new Style
                    {
                        width = 320,
                        height = 220,
                        backgroundColor = Color.red,
                        paddingTop = 16,
                        paddingLeft = 16,
                    },
                    children: new[]
                    {
                        P.Component<PineToolkitBehaviourProbe>(probe =>
                        {
                            behaviour = probe;
                            P.Cleanup(() => probe.Cleaned = true);
                            return P.Label(
                                text: "Ready",
                                reference: native => probe.Label = native,
                                style: new Style { display = DisplayStyle.None }
                            );
                        }),
                        P.Label(
                            text: "Pine UI Toolkit",
                            name: "Title",
                            style: new Style { fontSize = 28, color = Color.white }
                        ),
                        P.Button(
                            text: "Native button",
                            name: "Button",
                            style: new Style { height = 50 }
                        ),
                        P.TextField(name: "Input", value: enteredText),
                        P.VisualElement(
                            name: "Drawing",
                            style: new Style { width = 50, height = 50 },
                            events: new[]
                            {
                                P.Draw(context =>
                                {
                                    paints++;
                                    var painter = context.painter2D;
                                    painter.fillColor = paintColor.Value;
                                    painter.BeginPath();
                                    painter.MoveTo(Vector2.zero);
                                    painter.LineTo(new Vector2(50, 0));
                                    painter.LineTo(new Vector2(50, 50));
                                    painter.LineTo(new Vector2(0, 50));
                                    painter.ClosePath();
                                    painter.Fill();
                                }),
                            }
                        ),
                    }
                ),
            options: new PanelOptions { Panel = P.PanelSettings(targetTexture: texture) }
        );
        yield return null;
        yield return null;
        Check(
            mount.Document != null && mount.Document.rootVisualElement.panel != null,
            "Code-only document attaches a native runtime panel"
        );
        Check(mount.Document.panelSettings.themeStyleSheet != null, "Code-only panel has a theme");
        Check(behaviour.AwakeReady, "Behaviour activation follows native property initialization");
        var textField = mount.Root.Q<TextField>("Input");
        textField.Focus();
        yield return null;
        var focused = textField.focusController.focusedElement as VisualElement;
        Check(
            focused != null && (ReferenceEquals(focused, textField) || textField.Contains(focused)),
            "Native text field receives focus"
        );
        using (
            var key = KeyDownEvent.GetPooled(
                new UnityEngine.Event
                {
                    type = EventType.KeyDown,
                    character = 'z',
                    keyCode = KeyCode.Z,
                }
            )
        )
        {
            var textInput = textField.Q<VisualElement>(TextField.textInputUssName);
            textInput = textInput.Q<TextElement>() ?? textInput;
            key.target = textInput;
            textInput.SendEvent(key);
        }
        yield return null;
        Check(
            enteredText.Value == "z",
            $"Native keyboard editing updates the writable source: native={textField.value}, source={enteredText.Value}"
        );
        Check(
            mount.Root.Q<Label>("Title").resolvedStyle.fontSize == 28,
            "Native label styles resolve"
        );
        Check(
            mount.Root.Q<Button>("Button").resolvedStyle.height > 0,
            "Themed native button lays out"
        );
        Check(paints > 0, "Native Painter2D callback executes");
        int previousPaints = paints;
        paintColor.Value = Color.green;
        yield return null;
        yield return null;
        Check(paints > previousPaints, "Tracked drawing sources request native repaint");
        var rows = P.Source<System.Collections.Generic.IReadOnlyList<string>>(
            new[] { "One", "Two" }
        );
        var list = P.Ref<ListView>();
        var tree = P.Ref<TreeView>();
        Column column = null;
        using (
            var collections = P.Mount(
                () =>
                    P.VisualElement(
                        style: new Style { display = DisplayStyle.None },
                        children: new[]
                        {
                            P.ListView(
                                reference: list,
                                rows: P.Rows(rows, (item, index) => P.Label(text: item))
                            ),
                            P.TreeView(
                                reference: tree,
                                rows: P.TreeRows(
                                    new[] { new TreeViewItemData<string>(42, "Root") },
                                    (item, index) => P.Label(text: item)
                                )
                            ),
                            P.MultiColumnListView(
                                rows: P.Rows(rows),
                                columns: new[]
                                {
                                    column = P.Column(
                                        title: "Name",
                                        cell: P.Cell<string>(item => P.Label(text: item))
                                    ),
                                }
                            ),
                        }
                    ),
                mount.Root
            )
        )
        {
            var row = list.Value.makeItem();
            list.Value.bindItem(row, 0);
            Check(((Label)row[0]).text == "One", "Native list templates work in the player");
            rows.Value = new[] { "Changed" };
            list.Value.bindItem(row, 0);
            Check(
                ((Label)row[0]).text == "Changed",
                "Native list data replacement and recycling work in the player"
            );
            var branch = tree.Value.makeItem();
            tree.Value.bindItem(branch, 0);
            Check(
                ((Label)branch[0]).text == "Root" && tree.Value.GetIdForIndex(0) == 42,
                "Native tree templates and IDs work in the player"
            );
            var cell = column.makeCell();
            column.bindCell(cell, 0);
            Check(
                ((Label)cell[0]).text == "Changed",
                "Native typed table cells work in the player"
            );
            collections.Dispose();
            Check(
                row.childCount == 0 && branch.childCount == 0 && cell.childCount == 0,
                "Player collection disposal releases live recycled scopes"
            );
        }
        var oldRoot = mount.Root;
        var oldBehaviour = behaviour;
        mount.Document.enabled = false;
        yield return null;
        Check(
            oldRoot.parent == null && mount.Root == null,
            "Disabling document releases stale tree"
        );
        Check(
            oldBehaviour == null || oldBehaviour.Cleaned,
            "Document suspension cleans owned behaviours"
        );
        mount.Document.enabled = true;
        yield return null;
        yield return null;
        Check(
            mount.Root != null && !ReferenceEquals(oldRoot, mount.Root),
            "Enabling document builds a new native root"
        );

        string capture = System.IO.Path.Combine(
            Application.persistentDataPath,
            "pine-toolkit-runtime.png"
        );
        yield return null;
        var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        RenderTexture.active = texture;
        image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        image.Apply();
        RenderTexture.active = previous;
        System.IO.File.WriteAllBytes(capture, image.EncodeToPNG());
        int red = 0,
            light = 0;
        foreach (var pixel in image.GetPixels32())
        {
            if (pixel.r > 180 && pixel.g < 100 && pixel.b < 100)
                red++;
            if (pixel.r > 180 && pixel.g > 180 && pixel.b > 180)
                light++;
        }
        Destroy(image);
        Check(red > 1000, "Native toolkit panel renders colored pixels");
        Check(light > 50, "Native text and controls render contrasting pixels");
        Debug.Log(
            $"PINE_TOOLKIT_RUNTIME_CHECKS_PASSED ({_checks} assertions); screenshot={capture}"
        );
        mount.Dispose();
        texture.Release();
        Destroy(texture);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(0);
#endif
    }
}

public sealed class PineToolkitBehaviourProbe : MonoBehaviour
{
    public Label Label;
    public bool AwakeReady;
    public bool Cleaned;

    private void Awake() => AwakeReady = Label != null && Label.text == "Ready";
}
