using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pine
{
    public static partial class UI
    {
        internal static void SetupSelectable(Selectable target)
        {
            var background = GetOrAdd<Image>(target.gameObject);
            background.color = new Color(0.12f, 0.35f, 0.24f); background.raycastTarget = true; target.targetGraphic = background;
            target.navigation = new Navigation { mode = UnityEngine.UI.Navigation.Mode.Automatic };
            BindValue(target, new Value<bool>(() => ReducedMotion.Value), (item, reduced) =>
            {
                ColorBlock colors = item.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
                colors.selectedColor = new Color(1.35f, 1.35f, 1.35f); colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f); colors.fadeDuration = reduced ? 0 : 0.08f; item.colors = colors;
            });
        }
        private static TextMeshProUGUI ControlLabel(Value<string> text)
            => Label(text, Stretch(), Configure<TextMeshProUGUI>(target => target.alignment = TextAlignmentOptions.Center));
        /// <summary>Creates a complete native Button with a background target graphic, centered TMP label, visible focus colors and an owned click handler. Text accepts literals or tracked getters. Mouse/touch and navigation submit are handled by Unity&#x27;s compatible input module.</summary>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="click">Owned callback invoked by native pointer or navigation submit.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Increment", () => count.Value++, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Button Button(Value<string> text, Action click, params IProperty<Button>[] properties)
        {
            var button = Create<Button>();
            Apply(button, Children(ControlLabel(text)), OnClick(click)); Apply(button, properties); return button;
        }
        /// <summary>Creates a complete native Button with a background target graphic, centered TMP label, visible focus colors and an owned click handler. Text accepts literals or tracked getters. Mouse/touch and navigation submit are handled by Unity&#x27;s compatible input module.</summary>
        /// <param name="text">The typed text input (Func&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="click">Owned callback invoked by native pointer or navigation submit.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Button("Increment", () => count.Value++, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Button Button(Func<string> text, Action click, params IProperty<Button>[] properties) => Button(new Value<string>(text), click, properties);
        /// <summary>Creates a complete native Toggle with background, checkmark and centered label. A Source&lt;bool&gt; provides two-way binding; Value&lt;bool&gt; provides source-to-control binding. Pine owns graphic wiring, default navigation and focus colors.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var enabled = UI.Source(false);
        /// UI.Toggle(enabled, "Enabled", UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Toggle Toggle(Source<bool> value, params IProperty<Toggle>[] properties) => Toggle(value, default(Value<string>), properties);
        /// <summary>Creates a complete native Toggle with background, checkmark and centered label. A Source&lt;bool&gt; provides two-way binding; Value&lt;bool&gt; provides source-to-control binding. Pine owns graphic wiring, default navigation and focus colors.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var enabled = UI.Source(false);
        /// UI.Toggle(enabled, "Enabled", UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Toggle Toggle(Source<bool> value, Value<string> text, params IProperty<Toggle>[] properties)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var toggle = Create<Toggle>();
            var mark = Image(Name("Checkmark"), Size(20, 20), Position(-60, 0), Tint(new Color(0.65f, 1f, 0.8f)));
            var label = ControlLabel(text);
            toggle.graphic = mark;
            Apply(toggle, Children(mark, label), ToggleValue(value)); Apply(toggle, properties); return toggle;
        }
        /// <summary>Creates a complete native Toggle with background, checkmark and centered label. A Source&lt;bool&gt; provides two-way binding; Value&lt;bool&gt; provides source-to-control binding. Pine owns graphic wiring, default navigation and focus colors.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var enabled = UI.Source(false);
        /// UI.Toggle(enabled, "Enabled", UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Toggle Toggle(Value<bool> value, params IProperty<Toggle>[] properties) => Toggle(value, default(Value<string>), properties);
        /// <summary>Creates a complete native Toggle with background, checkmark and centered label. A Source&lt;bool&gt; provides two-way binding; Value&lt;bool&gt; provides source-to-control binding. Pine owns graphic wiring, default navigation and focus colors.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var enabled = UI.Source(false);
        /// UI.Toggle(enabled, "Enabled", UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Toggle Toggle(Value<bool> value, Value<string> text, params IProperty<Toggle>[] properties)
        {
            var toggle = Create<Toggle>(); var mark = Image(Name("Checkmark"), Size(20, 20), Position(-60, 0));
            toggle.graphic = mark; Apply(toggle, Children(mark, ControlLabel(text)), Set<Toggle, bool>("Value", (target, next) => target.SetIsOnWithoutNotify(next), value));
            Apply(toggle, properties); return toggle;
        }
        /// <summary>Creates a complete native Slider with background, fill and handle. A Source&lt;float&gt; is two-way and normalizes to native clamping; Value&lt;float&gt; is one-way. Bounds accept reactive finite values, including negative minima; maximum must be at least minimum.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var volume = UI.Source(0.5f);
        /// UI.Slider(volume, 0f, 1f, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Slider Slider(Source<float> value, params IProperty<Slider>[] properties) => Slider(value, 0f, 1f, properties);
        /// <summary>Creates a complete native Slider with background, fill and handle. A Source&lt;float&gt; is two-way and normalizes to native clamping; Value&lt;float&gt; is one-way. Bounds accept reactive finite values, including negative minima; maximum must be at least minimum.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="minimum">Reactive finite lower bound; negative values are supported.</param>
        /// <param name="maximum">Reactive finite upper bound, at least the lower bound.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var volume = UI.Source(0.5f);
        /// UI.Slider(volume, 0f, 1f, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Slider Slider(Source<float> value, Value<float> minimum, Value<float>? maximum, params IProperty<Slider>[] properties)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var slider = Slider(new Value<float>(value.Peek()), minimum, maximum);
            Apply(slider, SliderValue(value)); Apply(slider, properties); return slider;
        }
        /// <summary>Creates a complete native Slider with background, fill and handle. A Source&lt;float&gt; is two-way and normalizes to native clamping; Value&lt;float&gt; is one-way. Bounds accept reactive finite values, including negative minima; maximum must be at least minimum.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var volume = UI.Source(0.5f);
        /// UI.Slider(volume, 0f, 1f, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Slider Slider(Value<float> value, params IProperty<Slider>[] properties) => Slider(value, 0f, 1f, properties);
        /// <summary>Creates a complete native Slider with background, fill and handle. A Source&lt;float&gt; is two-way and normalizes to native clamping; Value&lt;float&gt; is one-way. Bounds accept reactive finite values, including negative minima; maximum must be at least minimum.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="minimum">Reactive finite lower bound; negative values are supported.</param>
        /// <param name="maximum">Reactive finite upper bound, at least the lower bound.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var volume = UI.Source(0.5f);
        /// UI.Slider(volume, 0f, 1f, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static Slider Slider(Value<float> value, Value<float> minimum, Value<float>? maximum, params IProperty<Slider>[] properties)
        {
            var slider = Create<Slider>();
            var fill = Image(Name("Fill"), Stretch(), Tint(new Color(0.3f, 0.8f, 0.5f)));
            var handle = Image(Name("Handle"), Size(20, 40), Tint(Color.white));
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            Apply(slider, Children(fill, handle));
            Value<float> high = maximum ?? new Value<float>(1);
            Effect(() =>
            {
                float low = minimum.Read(), max = high.Read();
                if (float.IsNaN(low) || float.IsInfinity(low) || float.IsNaN(max) || float.IsInfinity(max)) throw new ArgumentOutOfRangeException(nameof(minimum), "Slider bounds must be finite.");
                if (max < low) throw new ArgumentException("Slider maximum must be at least its minimum.");
                Untrack(() => { slider.minValue = low; slider.maxValue = max; });
            });
            Apply(slider, Set<Slider, float>("Value", (target, next) => target.SetValueWithoutNotify(next), value));
            Apply(slider, properties); return slider;
        }
        /// <summary>Creates a complete native Scrollbar with a wired handle and target graphic. Source&lt;float&gt; binds two ways; Value&lt;float&gt; binds one way. Normalized size defaults to 0.2 and can be reactive.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var position = UI.Source(0f);
        /// UI.Scrollbar(position, UI.Size(240, 24));
        /// ]]></code>
        /// </example>
        public static Scrollbar Scrollbar(Source<float> value, params IProperty<Scrollbar>[] properties) => Scrollbar(value, new Value<float>(0.2f), properties);
        /// <summary>Creates a complete native Scrollbar with a wired handle and target graphic. Source&lt;float&gt; binds two ways; Value&lt;float&gt; binds one way. Normalized size defaults to 0.2 and can be reactive.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="size">The typed size input (Value&lt;float&gt;?); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var position = UI.Source(0f);
        /// UI.Scrollbar(position, UI.Size(240, 24));
        /// ]]></code>
        /// </example>
        public static Scrollbar Scrollbar(Source<float> value, Value<float>? size, params IProperty<Scrollbar>[] properties)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var bar = Scrollbar(new Value<float>(value.Peek()), size);
            Apply(bar, Set<Scrollbar, float>("Value", (target, next) => { target.SetValueWithoutNotify(next); value.Value = target.value; }, value), On<Scrollbar, float>(target => target.onValueChanged, next => value.Value = next));
            Apply(bar, properties); return bar;
        }
        /// <summary>Creates a complete native Scrollbar with a wired handle and target graphic. Source&lt;float&gt; binds two ways; Value&lt;float&gt; binds one way. Normalized size defaults to 0.2 and can be reactive.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var position = UI.Source(0f);
        /// UI.Scrollbar(position, UI.Size(240, 24));
        /// ]]></code>
        /// </example>
        public static Scrollbar Scrollbar(Value<float> value, params IProperty<Scrollbar>[] properties) => Scrollbar(value, new Value<float>(0.2f), properties);
        /// <summary>Creates a complete native Scrollbar with a wired handle and target graphic. Source&lt;float&gt; binds two ways; Value&lt;float&gt; binds one way. Normalized size defaults to 0.2 and can be reactive.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="size">The typed size input (Value&lt;float&gt;?); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var position = UI.Source(0f);
        /// UI.Scrollbar(position, UI.Size(240, 24));
        /// ]]></code>
        /// </example>
        public static Scrollbar Scrollbar(Value<float> value, Value<float>? size, params IProperty<Scrollbar>[] properties)
        {
            var bar = Create<Scrollbar>(); var handle = Image(Name("Handle"), Stretch(), Tint(Color.white));
            bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
            Apply(bar, Children(handle), Set<Scrollbar, float>("Size", (target, next) => target.size = Mathf.Clamp01(next), size ?? new Value<float>(0.2f)),
                Set<Scrollbar, float>("Value", (target, next) => target.SetValueWithoutNotify(next), value));
            Apply(bar, properties); return bar;
        }
        /// <summary>Creates a complete TMP_InputField with a clipped text viewport, text component, placeholder and caret. Source&lt;string&gt; binds two ways; Value&lt;string&gt; binds one way. Native Unity supplies keyboard/IME behavior, and properties remain typed to the field.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var playerName = UI.Source("");
        /// UI.TextField(playerName, "Name", UI.CharacterLimit(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_InputField TextField(Source<string> value, params IProperty<TMP_InputField>[] properties) => TextField(value, default(Value<string>), properties);
        /// <summary>Creates a complete TMP_InputField with a clipped text viewport, text component, placeholder and caret. Source&lt;string&gt; binds two ways; Value&lt;string&gt; binds one way. Native Unity supplies keyboard/IME behavior, and properties remain typed to the field.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="placeholder">The typed placeholder input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var playerName = UI.Source("");
        /// UI.TextField(playerName, "Name", UI.CharacterLimit(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_InputField TextField(Source<string> value, Value<string> placeholder, params IProperty<TMP_InputField>[] properties)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var field = TextField(new Value<string>(value.Peek()), placeholder);
            Apply(field, InputValue(value)); Apply(field, properties); return field;
        }
        /// <summary>Creates a complete TMP_InputField with a clipped text viewport, text component, placeholder and caret. Source&lt;string&gt; binds two ways; Value&lt;string&gt; binds one way. Native Unity supplies keyboard/IME behavior, and properties remain typed to the field.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var playerName = UI.Source("");
        /// UI.TextField(playerName, "Name", UI.CharacterLimit(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_InputField TextField(Value<string> value, params IProperty<TMP_InputField>[] properties) => TextField(value, default(Value<string>), properties);
        /// <summary>Creates a complete TMP_InputField with a clipped text viewport, text component, placeholder and caret. Source&lt;string&gt; binds two ways; Value&lt;string&gt; binds one way. Native Unity supplies keyboard/IME behavior, and properties remain typed to the field.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="placeholder">The typed placeholder input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var playerName = UI.Source("");
        /// UI.TextField(playerName, "Name", UI.CharacterLimit(24), UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_InputField TextField(Value<string> value, Value<string> placeholder, params IProperty<TMP_InputField>[] properties)
        {
            var field = Create<TMP_InputField>(); var viewport = Frame(Name("Text viewport"), Stretch(), Clip(true));
            var text = Label("", Stretch(), Configure<TextMeshProUGUI>(target => target.alignment = TextAlignmentOptions.MidlineLeft));
            var hint = Label(placeholder, Stretch(), Tint(new Color(1, 1, 1, 0.5f)), Configure<TextMeshProUGUI>(target => target.alignment = TextAlignmentOptions.MidlineLeft));
            Apply(viewport, Children(text, hint)); field.textViewport = viewport; field.textComponent = text; field.placeholder = hint;
            field.fontAsset = ResolveFont(); field.caretWidth = 2; field.customCaretColor = true; field.caretColor = Color.white;
            Apply(field, Children(viewport), Set<TMP_InputField, string>("Value", (target, next) => target.SetTextWithoutNotify(next ?? ""), value));
            Apply(field, properties); return field;
        }
        /// <summary>Creates a complete TMP_Dropdown with caption, inactive scrollable template, item toggle and label. Options and selection accept typed reactive inputs. Source&lt;int&gt; binds two ways and stays normalized when options change. Changed option names close an expanded native menu immediately; selection-only updates preserve its items. Reduced motion disables native fade travel.</summary>
        /// <param name="selected">The typed selected input (Source&lt;int&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="options">Typed reactive option labels in display order. Null is treated as an empty collection; changing names closes an expanded native menu to avoid stale choices.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var selection = UI.Source(0);
        /// UI.Dropdown(selection, new[] { "Low", "High" }, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_Dropdown Dropdown(Source<int> selected, Value<string[]> options, params IProperty<TMP_Dropdown>[] properties)
        {
            if (selected == null) throw new ArgumentNullException(nameof(selected));
            var dropdown = BuildDropdown(selected, options, selected);
            Apply(dropdown, On<TMP_Dropdown, int>(target => target.onValueChanged, value => selected.Value = value));
            Apply(dropdown, properties); return dropdown;
        }
        /// <summary>Creates a complete TMP_Dropdown with caption, inactive scrollable template, item toggle and label. Options and selection accept typed reactive inputs. Source&lt;int&gt; binds two ways and stays normalized when options change. Changed option names close an expanded native menu immediately; selection-only updates preserve its items. Reduced motion disables native fade travel.</summary>
        /// <param name="selected">The typed selected input (Value&lt;int&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="options">Typed reactive option labels in display order. Null is treated as an empty collection; changing names closes an expanded native menu to avoid stale choices.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// var selection = UI.Source(0);
        /// UI.Dropdown(selection, new[] { "Low", "High" }, UI.Size(240, 48));
        /// ]]></code>
        /// </example>
        public static TMP_Dropdown Dropdown(Value<int> selected, Value<string[]> options, params IProperty<TMP_Dropdown>[] properties)
        { var dropdown = BuildDropdown(selected, options, null); Apply(dropdown, properties); return dropdown; }
        private static TMP_Dropdown BuildDropdown(Value<int> selected, Value<string[]> options, Source<int> writable)
        {
            var dropdown = Create<TMP_Dropdown>();
            var caption = ControlLabel("");
            var template = Frame(Name("Dropdown template"), Size(160, 180), Pivot(new Vector2(0.5f, 1)), Position(0, -20));
            var scroll = Create<ScrollRect>(Stretch());
            var viewport = Frame(Name("Viewport"), Stretch(), Clip(true));
            var content = Column(Name("Content"), Width(160), AutoHeight(), Vertical(0), Pivot(new Vector2(0.5f, 1)), Anchors(new Vector2(0, 1), new Vector2(1, 1)));
            var item = Toggle(new Value<bool>(false), "", Name("Item"), Size(160, 32));
            var itemLabel = item.GetComponentInChildren<TextMeshProUGUI>();
            Apply(content, Children(item)); Apply(viewport, Children(content));
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            Apply(scroll, Children(viewport)); Apply(template, Children(scroll));
            dropdown.captionText = caption; dropdown.itemText = itemLabel; dropdown.template = template;
            Apply(dropdown, Children(caption, template)); template.gameObject.SetActive(false);
            string[] previousNames = null;
            Effect(() =>
            {
                string[] names = options.Read() ?? Array.Empty<string>(); int index = selected.Read(); bool reduced = ReducedMotion.Value;
                Untrack(() =>
                {
                    bool changed = previousNames == null || previousNames.Length != names.Length;
                    for (int i = 0; !changed && i < names.Length; i++) changed = previousNames[i] != names[i];
                    if (changed)
                    {
                        // Native menus clone their items. Deactivation closes them immediately so
                        // old labels cannot select indices from a replacement option collection.
                        if (dropdown.IsExpanded) { dropdown.enabled = false; dropdown.enabled = true; }
                        dropdown.ClearOptions(); dropdown.AddOptions(new List<string>(names));
                        previousNames = (string[])names.Clone();
                    }
                    dropdown.SetValueWithoutNotify(index); dropdown.RefreshShownValue();
                    if (writable != null) writable.Value = dropdown.value;
                    dropdown.alphaFadeSpeed = reduced ? 0 : 0.08f;
                });
            });
            return dropdown;
        }
        /// <summary>Creates a complete native ScrollRect with a clipped viewport and drag-receiving surface. Native content results attach without losing their identity; a construction getter builds content within a fill-width, auto-height host. Scrolling is vertical by default and axes can be configured explicitly.</summary>
        /// <param name="content">Native scroll content or scoped construction callback; the viewport is supplied internally.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.ScrollView(UI.Column(UI.AutoHeight(), UI.Children(UI.Label("Content"))), UI.Size(300, 120));
        /// ]]></code>
        /// </example>
        public static ScrollRect ScrollView(Component content, params IProperty<ScrollRect>[] properties)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var scroll = Create<ScrollRect>(); var viewport = Frame(Name("Viewport"), Stretch(), Clip(true));
            // Native viewport background receives pointer/drag events even when content is decorative.
            var surface = GetOrAdd<Image>(viewport.gameObject); surface.color = Color.clear; surface.raycastTarget = true;
            Apply(viewport, Children(content)); scroll.viewport = viewport; scroll.content = Require<RectTransform>(content.gameObject); scroll.horizontal = false; scroll.vertical = true;
            Apply(scroll, Children(viewport)); Apply(scroll, properties); return scroll;
        }
        /// <summary>Creates a complete native ScrollRect with a clipped viewport and drag-receiving surface. Native content results attach without losing their identity; a construction getter builds content within a fill-width, auto-height host. Scrolling is vertical by default and axes can be configured explicitly.</summary>
        /// <param name="content">Native scroll content or scoped construction callback; the viewport is supplied internally.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.ScrollView(UI.Column(UI.AutoHeight(), UI.Children(UI.Label("Content"))), UI.Size(300, 120));
        /// ]]></code>
        /// </example>
        public static ScrollRect ScrollView(Func<Component> content, params IProperty<ScrollRect>[] properties)
        {
            var host = Column(Name("Content"), FillWidth(), AutoHeight(), Pivot(new Vector2(0.5f, 1)), Anchors(new Vector2(0, 1), new Vector2(1, 1)), Children(() => new[] { content() }));
            return ScrollView(host, properties);
        }
        /// <summary>Creates a native horizontal filled Image backed by a generated white sprite. Progress is clamped between zero and one and supports typed reactive values/getters. Apply Tint, Size and other Image-compatible declarations normally.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Progress(() => health.Value / 100f, UI.Size(300, 20));
        /// ]]></code>
        /// </example>
        public static Image Progress(Value<float> value, params IProperty<Image>[] properties)
        {
            var image = Image(); image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            image.sprite = DefaultSprite;
            Apply(image, Set<Image, float>("Progress", (target, next) => target.fillAmount = Mathf.Clamp01(next), value)); Apply(image, properties); return image;
        }
        /// <summary>Creates a native horizontal filled Image backed by a generated white sprite. Progress is clamped between zero and one and supports typed reactive values/getters. Apply Tint, Size and other Image-compatible declarations normally.</summary>
        /// <param name="value">The typed value or reactive input to read/apply; sources remain observable for the owning lifetime.</param>
        /// <param name="properties">Compatible typed declarations to apply; incompatible component/property combinations are rejected at compilation.</param>
        /// <returns>The live native component, owned by the active scope. Retain it for direct native access or typed UI.Apply; reactive bindings update this same instance.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Progress(() => health.Value / 100f, UI.Size(300, 20));
        /// ]]></code>
        /// </example>
        public static Image Progress(Func<float> value, params IProperty<Image>[] properties) => Progress(new Value<float>(value), properties);
        /// <summary>Binds the text of a completely wired TMP_InputField placeholder. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="text">The typed text input (Value&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.TextField(playerName, UI.Placeholder("Name"));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_InputField> Placeholder(Value<string> text) => Set<TMP_InputField, string>("Placeholder", (target, value) => ((TMP_Text)target.placeholder).text = value, text);
        /// <summary>Binds the text of a completely wired TMP_InputField placeholder. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="text">The typed text input (Func&lt;string&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.TextField(playerName, UI.Placeholder("Name"));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_InputField> Placeholder(Func<string> text) => Placeholder(new Value<string>(text));
        /// <summary>Binds the native text-field character limit; zero means unlimited and negative limits are rejected. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="limit">The typed limit input (Value&lt;int&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.TextField(playerName, UI.CharacterLimit(24));
        /// ]]></code>
        /// </example>
        public static IProperty<TMP_InputField> CharacterLimit(Value<int> limit) => Set<TMP_InputField, int>("CharacterLimit", (target, value) => { if (value < 0) throw new ArgumentOutOfRangeException(nameof(limit)); target.characterLimit = value; }, limit);
        /// <summary>Binds the native slider direction. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="direction">The typed direction input (Value&lt;Slider.Direction&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Slider(volume, UI.SliderDirection(UnityEngine.UI.Slider.Direction.BottomToTop));
        /// ]]></code>
        /// </example>
        public static IProperty<Slider> SliderDirection(Value<Slider.Direction> direction) => Set<Slider, Slider.Direction>("Direction", (target, value) => target.direction = value, direction);
        /// <summary>Binds whether the native slider rounds values to whole numbers. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="enabled">The typed enabled input (Value&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Slider(count, 0f, 10f, UI.WholeNumbers(true));
        /// ]]></code>
        /// </example>
        public static IProperty<Slider> WholeNumbers(Value<bool> enabled) => Set<Slider, bool>("WholeNumbers", (target, value) => target.wholeNumbers = value, enabled);
        /// <summary>Binds the native scrollbar direction. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="direction">The typed direction input (Value&lt;Scrollbar.Direction&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.Scrollbar(position, UI.ScrollbarDirection(UnityEngine.UI.Scrollbar.Direction.BottomToTop));
        /// ]]></code>
        /// </example>
        public static IProperty<Scrollbar> ScrollbarDirection(Value<Scrollbar.Direction> direction) => Set<Scrollbar, Scrollbar.Direction>("Direction", (target, value) => target.direction = value, direction);
        /// <summary>Binds horizontal and vertical scroll-axis enablement. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="horizontal">The typed horizontal input (Value&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <param name="vertical">The typed vertical input (Value&lt;bool&gt;); literals and supported reactive adapters follow this overload&#x27;s documented behavior.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.ScrollView(content, UI.ScrollAxes(false, true));
        /// ]]></code>
        /// </example>
        public static IProperty<ScrollRect> ScrollAxes(Value<bool> horizontal, Value<bool> vertical) => Group<ScrollRect>(Set<ScrollRect, bool>("Horizontal", (target, value) => target.horizontal = value, horizontal), Set<ScrollRect, bool>("Vertical", (target, value) => target.vertical = value, vertical));
        /// <summary>Binds native normalized scroll position; layout and movement settings remain native ScrollRect behavior. Literal values apply once; typed reactive values and Value-wrapped getters stay bound for the active ownership scope.</summary>
        /// <param name="position">Typed immediate position or reactive native position, as specified by this overload.</param>
        /// <returns>A compatible property operation to apply within a live ownership scope.</returns>
        /// <remarks>Construct and apply declarations on Unity's main thread within UI.Mount, UI.Root or a live Scope.Run. Literal assignments occur once; reactive observers and handlers end with their owning scope.</remarks>
        /// <example>
        /// <code><![CDATA[
        /// UI.ScrollView(content, UI.ScrollPosition(new UnityEngine.Vector2(0, 1)));
        /// ]]></code>
        /// </example>
        public static IProperty<ScrollRect> ScrollPosition(Value<Vector2> position) => Set<ScrollRect, Vector2>("Position", (target, value) => target.normalizedPosition = value, position);
        private static Sprite _defaultSprite;
        private static Sprite DefaultSprite
        {
            get
            {
                if (_defaultSprite == null) _defaultSprite = UnityEngine.Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                return _defaultSprite;
            }
        }
    }
}
