using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cards.Editor
{
    [CustomPropertyDrawer(typeof(Cards.Effects.ICardEffect), true)]
    public class CardEffectDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // Header/Dropdown height
            
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                // Calculate height for all child properties
                SerializedProperty iterator = property.Copy();
                bool enterChildren = true;
                
                // Advance to the first child
                if (iterator.NextVisible(enterChildren))
                {
                    do
                    {
                        // Stop if we've moved past the children of our main property
                        if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                            break;
                            
                        height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                        enterChildren = false; // Only top-level children, GetPropertyHeight handles nested
                    }
                    while (iterator.NextVisible(enterChildren));
                }
            }
            
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string typeName = property.managedReferenceFullTypename;
            string displayTypeName = "Null (Select Effect)";
            
            if (!string.IsNullOrEmpty(typeName))
            {
                int lastSpace = typeName.LastIndexOf(' ');
                if (lastSpace >= 0)
                {
                    string fullClassPath = typeName.Substring(lastSpace + 1);
                    int lastDot = fullClassPath.LastIndexOf('.');
                    displayTypeName = lastDot >= 0 ? fullClassPath.Substring(lastDot + 1) : fullClassPath;
                }
            }

            // Draw header rect
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            // Draw foldout if we have a value
            bool hasValue = property.managedReferenceValue != null;
            if (hasValue)
            {
                Rect foldoutRect = new Rect(headerRect.x, headerRect.y, 15, headerRect.height);
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);
            }

            // Draw Dropdown button next to foldout
            Rect dropdownRect = new Rect(headerRect.x + (hasValue ? 15 : 0), headerRect.y, headerRect.width - (hasValue ? 15 : 0), headerRect.height);
            
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(displayTypeName), FocusType.Keyboard))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("None"), string.IsNullOrEmpty(typeName), () =>
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                });

                var effectTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(Cards.Effects.ICardEffect).IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

                foreach (var type in effectTypes)
                {
                    menu.AddItem(new GUIContent(type.Name), typeName.Contains(type.FullName), () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.isExpanded = true; // Auto-expand when a type is selected
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

            // Draw children if expanded
            if (hasValue && property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                Rect childRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
                
                SerializedProperty iterator = property.Copy();
                bool enterChildren = true;
                
                if (iterator.NextVisible(enterChildren))
                {
                    do
                    {
                        if (SerializedProperty.EqualContents(iterator, property.GetEndProperty()))
                            break;
                            
                        float childHeight = EditorGUI.GetPropertyHeight(iterator, true);
                        childRect.height = childHeight;
                        
                        EditorGUI.PropertyField(childRect, iterator, true);
                        
                        childRect.y += childHeight + EditorGUIUtility.standardVerticalSpacing;
                        enterChildren = false;
                    }
                    while (iterator.NextVisible(enterChildren));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
