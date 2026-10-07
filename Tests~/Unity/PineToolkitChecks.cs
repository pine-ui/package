#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Pine;
using Pine.UIToolkit;
using UnityEditor;
using UnityEngine.UIElements;

public static class PineToolkitChecks
{
    private static int _checks;

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
        _checks++;
    }

    public static async System.Threading.Tasks.Task<int> SerializedBindingAsync()
    {
        _checks = 0;
        var window = UnityEngine.ScriptableObject.CreateInstance<PineToolkitTestWindow>();
        var model = UnityEngine.ScriptableObject.CreateInstance<PineToolkitSerializedModel>();
        model.Number = 4;
        window.Show();
        try
        {
            using var serialized = new SerializedObject(model);
            var field = P.Ref<IntegerField>();
            using var mount = P.Mount(
                () =>
                    P.VisualElement(
                        bindings: new[] { P.SerializedBinding(serialized) },
                        children: new[] { P.IntegerField(bindingPath: "Number", reference: field) }
                    ),
                window.rootVisualElement
            );
            await System.Threading.Tasks.Task.Delay(500);
            Check(field.Value.value == 4, "Native SerializedObject binding reads the model");
            field.Value.value = 8;
            await System.Threading.Tasks.Task.Delay(500);
            Check(model.Number == 8, "Native SerializedObject binding writes the model");
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            await System.Threading.Tasks.Task.Delay(500);
            Check(
                model.Number == 4 && field.Value.value == 4,
                "Native binding retains Editor Undo behavior"
            );
            var retainedField = field.Value;
            mount.Dispose();
            Check(
                ((IBindable)retainedField).binding == null,
                "Owned serialized binding is removed on disposal"
            );
            UnityEngine.Debug.Log($"PINE_TOOLKIT_SERIALIZED_CHECKS_PASSED ({_checks} assertions)");
            return _checks;
        }
        finally
        {
            window.Close();
            UnityEngine.Object.DestroyImmediate(model);
        }
    }

    public static int Run()
    {
        _checks = 0;
        var window = UnityEngine.ScriptableObject.CreateInstance<PineToolkitTestWindow>();
        window.Show();
        try
        {
            var parent = window.rootVisualElement;
            var external = new Label("External");
            parent.Add(external);
            var text = P.Source("Before");
            var width = P.Source(120);
            var fieldValue = P.Source(3);
            var label = P.Ref<Label>();
            var field = P.Ref<IntegerField>();
            int changes = 0;
            using var mount = P.Mount(
                () =>
                    P.VisualElement(
                        children: new[]
                        {
                            P.Label(
                                text: text,
                                reference: label,
                                style: new Style { width = width }
                            ),
                            P.IntegerField(
                                value: fieldValue,
                                reference: field,
                                onValueChanged: _ => changes++
                            ),
                        }
                    ),
                parent
            );
            Check(label.Value.text == "Before", "Initial source text");
            text.Value = "After";
            width.Value = 200;
            fieldValue.Value = 4;
            Check(
                label.Value.text == "After" && label.Value.style.width.value.value == 200,
                "Reactive native text and int-to-length style"
            );
            Check(field.Value.value == 4 && changes == 0, "Model writes are silent");
            field.Value.value = 7;
            Check(
                fieldValue.Value == 7 && changes == 1,
                "Native field writes update the source once"
            );
            var nested = new IntegerField();
            field.Value.Add(nested);
            nested.value = 19;
            Check(
                fieldValue.Value == 7 && changes == 1,
                "Bubbled child field events do not overwrite parent state"
            );
            VisualElement original = mount.Root;
            original.RemoveFromHierarchy();
            parent.Add(original);
            text.Value = "Reparented";
            Check(label.Value.text == "Reparented", "Temporary detach preserves ownership");
            mount.Dispose();
            mount.Dispose();
            Check(
                parent.childCount == 1 && parent[0] == external,
                "External host children survive disposal"
            );
            Check(label.Value == null && field.Value == null, "References clear on disposal");

            var selected = P.Source(true);
            var first = P.Source("A");
            var second = P.Source("B");
            int disposed = 0;
            View item(Source<string> source) =>
                P.Component(() =>
                {
                    P.Cleanup(() => disposed++);
                    return P.Label(text: source);
                });
            View a = item(first),
                b = item(second);
            using var dynamic = P.Mount(
                () => P.VisualElement(children: () => selected.Value ? new[] { a } : new[] { b }),
                parent
            );
            selected.Value = false;
            Check(
                disposed == 1 && ((Label)dynamic.Root[0]).text == "B",
                "Removed components dispose once"
            );
            first.Value = "Old";
            Check(
                ((Label)dynamic.Root[0]).text == "B",
                "Removed sources do not update replacements"
            );
            dynamic.Dispose();
            Check(disposed == 2, "Current component disposes with mount");

            int rowCleanup = 0;
            var list = P.Ref<ListView>();
            var oldValue = P.Source("One");
            var newValue = P.Source("Two");
            IReadOnlyList<Source<string>> data = new[] { oldValue, newValue };
            using var table = P.Mount(
                () =>
                    P.ListView(
                        reference: list,
                        rows: P.Rows(
                            () => data,
                            (value, index) =>
                            {
                                P.Cleanup(() => rowCleanup++);
                                return P.Label(text: value);
                            }
                        )
                    ),
                parent
            );
            var shell = list.Value.makeItem();
            list.Value.bindItem(shell, 0);
            Check(((Label)shell[0]).text == "One", "Native row binding");
            list.Value.bindItem(shell, 1);
            Check(
                rowCleanup == 1 && ((Label)shell[0]).text == "Two",
                "Recycle disposes previous row"
            );
            oldValue.Value = "Old";
            Check(((Label)shell[0]).text == "Two", "Recycled row no longer observes old data");
            table.Dispose();
            Check(
                rowCleanup == 2 && shell.childCount == 0,
                "Collection disposal releases live rows without native unbind"
            );

            var tree = P.Ref<TreeView>();
            using var forest = P.Mount(
                () =>
                    P.TreeView(
                        reference: tree,
                        rows: P.TreeRows(
                            new[] { new TreeViewItemData<string>(42, "Root") },
                            (value, index) => P.Label(text: value)
                        )
                    ),
                parent
            );
            var treeShell = tree.Value.makeItem();
            tree.Value.bindItem(treeShell, 0);
            Check(
                ((Label)treeShell[0]).text == "Root" && tree.Value.GetIdForIndex(0) == 42,
                "Native tree IDs and typed row data"
            );
            forest.Dispose();
            Check(treeShell.childCount == 0, "Tree disposal releases its live row");

            var multi = P.Ref<MultiColumnListView>();
            Column column = null;
            using var grid = P.Mount(
                () =>
                    P.MultiColumnListView(
                        reference: multi,
                        columns: new[]
                        {
                            column = P.Column(
                                title: "Name",
                                cell: P.Cell<Source<string>>((value, index) => P.Label(text: value))
                            ),
                        },
                        rows: P.Rows(() => data)
                    ),
                parent
            );
            var cellShell = column.makeCell();
            column.bindCell(cellShell, 0);
            Check(((Label)cellShell[0]).text == "Old", "Typed native table cell");
            column.bindCell(cellShell, 1);
            oldValue.Value = "Ignored";
            Check(
                ((Label)cellShell[0]).text == "Two",
                "Recycled table cells release old dependencies"
            );
            grid.Dispose();
            bool competingCellWriter = false;
            try
            {
                P.Root(() =>
                        P.Column(
                            makeCellValue: P.Source<Func<VisualElement>>(null),
                            cell: P.Cell<string>(text => P.Label(text: text))
                        )
                    )
                    .Dispose();
            }
            catch (InvalidOperationException)
            {
                competingCellWriter = true;
            }
            Check(
                competingCellWriter,
                "Cell templates reject reactive native callback writers even when initially null"
            );
            Check(
                cellShell.childCount == 0 && column.makeCell == null,
                "Table disposal releases cells and callbacks"
            );
            var columnWidth = P.Source(140f);
            P.Root(() =>
                {
                    var configured = P.Column(width: columnWidth);
                    Check(
                        configured.width.value == 140,
                        "Native Length props accept numeric sources"
                    );
                    columnWidth.Value = 180;
                    Check(configured.width.value == 180, "Native Length props update reactively");
                    var binding = P.DataBinding(dataSourcePath: "Number");
                    Check(
                        binding.dataSourcePath.ToString() == "Number",
                        "Native PropertyPath props accept strings"
                    );
                })
                .Dispose();

            int received = 0;
            Action<MeshGenerationContext> callback = _ => received++;
            var borrowed = new VisualElement { name = "Target" };
            Action<MeshGenerationContext> originalDrawing = _ => { };
            borrowed.generateVisualContent = originalDrawing;
            parent.Add(borrowed);
            using var native = P.Mount(
                () =>
                    P.Element(
                        () =>
                        {
                            var root = new VisualElement();
                            root.Add(borrowed);
                            return root;
                        },
                        targets: new[]
                        {
                            P.Target("Target", P.VisualElement(generateVisualContent: callback)),
                        }
                    ),
                parent
            );
            Check(borrowed.generateVisualContent != null, "Native delegate property installed");
            native.Dispose();
            Check(
                ReferenceEquals(borrowed.generateVisualContent, originalDrawing),
                "Native callback cleanup restores the prior callback"
            );
            parent.Add(borrowed);

            bool duplicate = false;
            try
            {
                P.Mount(() => P.Label(text: "Text", value: "Value"), parent);
            }
            catch (InvalidOperationException)
            {
                duplicate = true;
            }
            Check(
                duplicate && parent.childCount == 2,
                "Alias writers are rejected without leaving an owned tree"
            );
            parent.Remove(borrowed);

            var nativeBinding = new DataBinding();
            var replacement = new DataBinding();
            var bound = new VisualElement();
            using var bindingMount = P.Mount(
                () => P.Element(() => bound, bindings: new[] { P.Bind("tooltip", nativeBinding) }),
                parent
            );
            Check(
                ReferenceEquals(bound.GetBinding("tooltip"), nativeBinding),
                "Native runtime binding installed"
            );
            bound.SetBinding("tooltip", replacement);
            bindingMount.Dispose();
            Check(
                ReferenceEquals(bound.GetBinding("tooltip"), replacement),
                "Cleanup preserves externally replaced native bindings"
            );
            bound.ClearBinding("tooltip");
            var aliasLabel = new Label { name = "Alias" };
            var aliasBinding = new DataBinding();
            aliasLabel.SetBinding("value", aliasBinding);
            bool aliasConflict = false;
            try
            {
                P.Mount(
                    () =>
                        P.Element(
                            () =>
                            {
                                var root = new VisualElement();
                                root.Add(aliasLabel);
                                return root;
                            },
                            targets: new[] { P.Target("Alias", P.Label(text: "Competing")) }
                        ),
                    parent
                );
            }
            catch (InvalidOperationException)
            {
                aliasConflict = true;
            }
            Check(
                aliasConflict && ReferenceEquals(aliasLabel.GetBinding("value"), aliasBinding),
                "External text/value binding aliases reject competing writers and survive rejection"
            );
            aliasLabel.ClearBinding("value");
            bool passwordConflict = false;
            try
            {
                P.Mount(
                    () =>
                        P.TextField(
                            isPasswordField: true,
                            textEdition: P.TextEdition(isPassword: false)
                        ),
                    parent
                );
            }
            catch (InvalidOperationException)
            {
                passwordConflict = true;
            }
            Check(
                passwordConflict,
                "Native field and text-edition aliases reject competing password writers"
            );

            var clamped = P.Source(30f);
            var slider = P.Ref<Slider>();
            using var sliderMount = P.Mount(
                () => P.Slider(lowValue: 0f, highValue: 10f, value: clamped, reference: slider),
                parent
            );
            Check(
                clamped.Value == slider.Value.value && clamped.Value == 10f,
                "Native field normalization reconciles the source"
            );
            sliderMount.Dispose();
            var nativeColor = P.Source(UnityEngine.Color.red);
            var colorField = P.Ref<UnityEditor.UIElements.ColorField>();
            using var colors = P.Mount(
                () => P.ColorField(value: nativeColor, reference: colorField),
                parent
            );
            colorField.Value.value = UnityEngine.Color.green;
            Check(
                nativeColor.Value == UnityEngine.Color.green,
                "Native conversion adapters retain writable source identity"
            );
            colors.Dispose();

            var model = UnityEngine.ScriptableObject.CreateInstance<PineToolkitSerializedModel>();
            try
            {
                using var serialized = new SerializedObject(model);
                bool serializedConflict = false;
                try
                {
                    P.Mount(
                        () =>
                            P.VisualElement(
                                bindings: new[] { P.SerializedBinding(serialized) },
                                children: new[]
                                {
                                    P.IntegerField(bindingPath: "Number", value: P.Source(2)),
                                }
                            ),
                        parent
                    );
                }
                catch (InvalidOperationException)
                {
                    serializedConflict = true;
                }
                Check(
                    serializedConflict && parent.childCount == 1,
                    "Serialized bindings reject child source writers after tree construction"
                );
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
            const string templatePath = "Assets/PineToolkitChecks.uxml";
            System.IO.File.WriteAllText(
                templatePath,
                "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:Label name=\"SharedName\" text=\"Native\" /></ui:UXML>"
            );
            AssetDatabase.ImportAsset(templatePath, ImportAssetOptions.ForceSynchronousImport);
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
            var firstTemplateText = P.Source("First");
            using var firstTemplate = P.Mount(
                () =>
                    P.Template(
                        template,
                        targets: new[] { P.Target("SharedName", P.Label(text: firstTemplateText)) }
                    ),
                parent
            );
            using var secondTemplate = P.Mount(
                () =>
                    P.Template(
                        template,
                        targets: new[] { P.Target("SharedName", P.Label(text: "Second")) }
                    ),
                parent
            );
            firstTemplateText.Value = "Changed";
            Check(
                firstTemplate.Root.Q<Label>("SharedName").text == "Changed"
                    && secondTemplate.Root.Q<Label>("SharedName").text == "Second",
                "UXML targets stay local to each cloned instance"
            );
            var showChild = P.Source(true);
            var child = P.Label(text: "Child");
            using var reactiveTemplate = P.Mount(
                () =>
                    P.Template(
                        template,
                        children: () => showChild.Value ? new[] { child } : Array.Empty<View>()
                    ),
                parent
            );
            Check(
                reactiveTemplate.Root.childCount == 2,
                "UXML clones accept reactive declared children"
            );
            showChild.Value = false;
            Check(
                reactiveTemplate.Root.childCount == 1,
                "Reactive template cleanup retains the native cloned content"
            );
            reactiveTemplate.Dispose();
            showChild.Value = true;
            using var customChildren = P.Mount(
                () =>
                    P.Element(
                        () => new VisualElement(),
                        children: () => showChild.Value ? new[] { child } : Array.Empty<View>()
                    ),
                parent
            );
            showChild.Value = false;
            Check(
                customChildren.Root.childCount == 0,
                "Custom native controls accept reactive children and cleanup"
            );
            customChildren.Dispose();
            var staticChildren = new[] { P.Label(text: "Snapshot") };
            var snapshotDeclaration = P.VisualElement(children: staticChildren);
            staticChildren[0] = P.Label(text: "Changed");
            using var snapshot = P.Mount(() => snapshotDeclaration, parent);
            Check(
                ((Label)snapshot.Root[0]).text == "Snapshot",
                "Static child declarations snapshot their array"
            );
            snapshot.Dispose();
            firstTemplate.Dispose();
            secondTemplate.Dispose();
            AssetDatabase.DeleteAsset(templatePath);

            var eventOrder = new List<string>();
            var eventRoot = P.Ref<VisualElement>();
            var eventChild = P.Ref<VisualElement>();
            using var eventMount = P.Mount(
                () =>
                    P.VisualElement(
                        reference: eventRoot,
                        events: new[]
                        {
                            P.On<PointerDownEvent>(
                                _ => eventOrder.Add("trickle"),
                                TrickleDown.TrickleDown
                            ),
                            P.On<PointerDownEvent>(_ => eventOrder.Add("bubble")),
                        },
                        children: new[]
                        {
                            P.VisualElement(
                                reference: eventChild,
                                events: new[]
                                {
                                    P.On<PointerDownEvent>(_ => eventOrder.Add("target")),
                                }
                            ),
                        }
                    ),
                parent
            );
            var retainedChild = eventChild.Value;
            var retainedRoot = eventRoot.Value;
            int externalEvents = 0;
            retainedRoot.RegisterCallback<PointerDownEvent>(_ => externalEvents++);
            using (var ev = PointerDownEvent.GetPooled())
            {
                ev.target = retainedChild;
                retainedChild.SendEvent(ev);
            }
            Check(
                string.Join(",", eventOrder) == "trickle,target,bubble",
                "Native trickle and bubble ordering is preserved: " + string.Join(",", eventOrder)
            );
            eventMount.Dispose();
            parent.Add(retainedRoot);
            retainedRoot.Add(retainedChild);
            using (var ev = PointerDownEvent.GetPooled())
            {
                ev.target = retainedChild;
                retainedChild.SendEvent(ev);
            }
            Check(
                eventOrder.Count == 3 && externalEvents == 2,
                "Disposal removes Pine callbacks and preserves external listeners"
            );
            parent.Remove(retainedRoot);
            UnityEngine.Debug.Log($"PINE_TOOLKIT_CHECKS_PASSED ({_checks} assertions)");
            return _checks;
        }
        finally
        {
            window.Close();
        }
    }
}

public sealed class PineToolkitTestWindow : EditorWindow { }

public sealed class PineToolkitSerializedModel : UnityEngine.ScriptableObject
{
    public int Number;
}
#endif
