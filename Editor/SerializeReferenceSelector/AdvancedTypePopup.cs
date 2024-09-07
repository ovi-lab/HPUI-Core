using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace ubco.ovilab.HPUI.Editor
{
    /// <summary>
    /// Items used in <see cref="AdvancedTypePopup"/>
    /// </summary>
    public class AdvancedTypePopupItem : AdvancedDropdownItem
    {
        /// <summary>
        /// The <see cref="Type"/> representing the item.
        /// </summary>
        public Type Type { get; }

        public AdvancedTypePopupItem (Type type,string name) : base(name)
        {
            Type = type;
        }
    }

    /// <summary>
    /// A type popup with a fuzzy finder.
    /// </summary>
    /// This is taken from https://github.com/mackysoft/Unity-SerializeReferenceExtensions
    public class AdvancedTypePopup : AdvancedDropdown
    {
        private string emptyDisplayName;
        private Type[] types;

        public event Action<AdvancedTypePopupItem> OnItemSelected;
		
        public AdvancedTypePopup (IEnumerable<Type> types, int maxLineCount, string emptyDisplayName,  AdvancedDropdownState state) : base(state)
        {
            this.emptyDisplayName = emptyDisplayName;
            this.types = types.ToArray();
            minimumSize = new Vector2(minimumSize.x,EditorGUIUtility.singleLineHeight * maxLineCount + EditorGUIUtility.singleLineHeight * 2f);
        }

        /// <inheritdoc />
        protected override AdvancedDropdownItem BuildRoot ()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Select Type");
            int itemCount = 0;

            // Add null item.
            AdvancedTypePopupItem nullItem = new AdvancedTypePopupItem(null, emptyDisplayName)
            {
                id = itemCount++
            };

            root.AddChild(nullItem);

            // Add type items.
            foreach (Type type in types)
            {
                AdvancedDropdownItem parent = root;

                string typeDisplayName = ObjectNames.NicifyVariableName(type.Name);
                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    typeDisplayName += $" ({type.Namespace})";
                }

                // Add type item.
                AdvancedTypePopupItem item = new AdvancedTypePopupItem(type, typeDisplayName)
                {
                    id = itemCount++
                };
                parent.AddChild(item);
            }
            return root;
        }

        /// <inheritdoc />
        protected override void ItemSelected (AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (item is AdvancedTypePopupItem typePopupItem)
            {
                OnItemSelected?.Invoke(typePopupItem);
            }
        }
    }
}
