using UnityEngine;
using System;

namespace HPUI.utils
{
    public static class Extensions
    {
        /// <summary>
        /// Performs a depth-first search of the transforms associated to the given transform, in search
        /// of a descendant with the given name.  Avoid using this method on a frame-by-frame basis, as
        /// it is recursive and quite capable of being slow!
        /// </summary>
        /// <param name="searchTransform">Transform to search within</param>
        /// <param name="descendantName">Name of the descendant to find</param>
        /// <returns>Descendant transform if found, otherwise null.</returns>
        public static Transform FindDescendentTransform(this Transform searchTransform, string descendantName)
        {
            Transform result = null;
 
            int childCount = searchTransform.childCount;
            result = searchTransform.Find(descendantName);
            if (result)
                return result;
        
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = searchTransform.GetChild(i);
 
                // Not it, but has children? Search the children.
                if (childTransform.childCount > 0)
                {
                    Transform grandchildTransform = FindDescendentTransform(childTransform, descendantName);
                    if (grandchildTransform == null)
                        continue;
 
                    result = grandchildTransform;
                    break;
                }
                // Not it, but has no children?  Go on to the next sibling.
                else if (childTransform.childCount == 0)
                {
                    continue;
                }
            }
 
            return result;
        }
    }
}
