# HPUI-Core
## Usage
### Prefabs
#### DeformableSurface
- Set root's `TransformLinker` parent to the `palmbase` of hand
- Configure `width`, `height` and `HandCoordinateManager` (a hand proxy) of `DeformableCoordinateManager`
- Set the `HandCoordinateManager` (a hand proxy) in `PlaneMeshGenerator` of the `Surface` GameObject
- Configure parents in `TransformLinker` of the `PlaneMeshTransformAnchors` GameObject
- Configure the offset by modifying the transform of the `offset` GameObject
#### OVRHandCoordinateProxyR
- Represents the proxy for the right hand when used with the OVRCustomHandPrefab from the oculus plugin.
- Drop this into the scene, and set the `SkeletonRoot` to the `OVRCustomHandPrefab` representing the right hand.
- Set the `TransformLinker`'s parent of `PalmBase_base` to `b_r_wrist` on the `OVRCustomHandPrefab` representing the right hand.
### Scripts
#### InteractionManager
- Have a GameObject in the scene with the `InteractionManager` as a component for the `btn_container.prefab` to work.
#### HandCoordinateManager
- Use this to abstract the platform specific details of the hand coordinates used by HPUI. See `OVRHandCoordinateProxyR` for example.
- Set the `SkeletonRoot` to the GameObject that represents all the joints of the hand
- Set the GameObject representing the palm base as `PalmBase`
- Under `ManagedCoordinates`, list all the locations you may track.
- Under `ProxyToSeletonNameMapping`, add the proxyname and corresponding name on the skeleton being abstracted.

## TODO

- Use HandsManager instead of directly using the HandCoordinateManager in the DeformableSurface scripts
