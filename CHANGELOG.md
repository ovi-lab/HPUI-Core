# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0]
### Changed
- `HPUIGestureLogic.ComputeInteraction` now returns interactor-level `HPUIInteractorGestureEventArgs` while taking per-interactable `Dictionary<HPUIBaseInteractable, HPUIGestureState>` and `Dictionary<HPUIBaseInteractable, HPUIInteractableState>` collections for state emission.
- `HPUIGestureLogic.UpdateThresholds` and the gesture constructor now use the public threshold properties, including the tracking-switch threshold, instead of setting backing fields directly.
- `HPUIInteractor` now logs exceptions from interaction processing and event callbacks with `Debug.LogError` instead of swallowing them silently.
- The default hand pose setup was adjusted in the `HPUIInteractor` prefab and joint data, including `AlwaysReportPositionInStateEvents` and several joint offset values for the left and right index, middle, and ring fingers.
- Sample content now includes the missing event callback wiring in the HPUI sample scene.

### Fixed
- `HPUIInteractor` no longer marks a previous tracked interactable as `TrackingEnded` unless it still exists in `interactableEventStates`.
- Raycast centroid computation now uses inverse-distance weighting and `EPSILON` to avoid divide-by-zero when multiple hits land on the same point.
- Gesture state/event naming was corrected, including `HPUIInteractableState` and related callback names.
- Tap-related gesture API references were removed from the codebase.

## [0.3.0]
### Added
- Refactor gesture event and state handling in `HPUIGestureState`, `HPUIInteractorGestureEvent`, `HPUIInteractorGestureEventArgs`, `HPUIInteractableState`, and `HPUIInteractorGestureEventArgs` flow.
- Added `AlwaysReportPositionInStateEvents` to `HPUIGestureLogic` for configurable position reporting in per-interactable state events.
- Added internal `gestureEventStates` and `interactableEventStates` dictionaries to support per-interactable lifecycle tracking.
- Added tests for the new `HPUIGestureLogic.ComputeInteraction` signature and per-interactable event emission.
- Added support for cancelling active gestures via the `ErrorReset` helper and the gesture commit delay flow.
- Added `HPUIBaseInteractable` tracking safeguards so `TrackingEnded` is only applied when the interactable is still present in `interactableEventStates`.
- Added inverse-distance weighted raycast centroid computation and `Debug.Assert` guard for `totalWeight`.
- Added `JointFollower`-based dynamic cone ray detection and related skeleton driver improvements.
- Added mesh continuous collider manager bursting support.

### Changed
- Refactored gesture logic to separate interactor-level summarization from per-interactable event delivery in `HPUIGestureLogic.ComputeInteraction`.
- Renamed the auxiliary gesture event callback to `OnInteractableStateEvent` and updated remaining callers.
- Reworked gesture API surface to remove tap support and simplify the gesture lifecycle.
- Updated the joint pose approximation documentation and editor exposure for mesh sigma and approximation parameters.
- Moved the documentation site to a separate repository.
- Improved `HPUIInteractor` and related test coverage for gesture lifecycle handling.

## [0.2.0]
### Added
- Complete refactor to use [XRI](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/index.html) and [XR Hands](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.5/manual/index.html).
  - Adds HPUI Interactor and related logic implementations
  - Adds HPUI Interactables
    - `HPUIBaseInteractable` - desrete targets
    - `HPUIGeneratedContinuousInteractable` - deformable continuous interactable which can be generated at runtime
    - `HPUIMeshContinuousInteractable` - deformable continuous interactable that uses existing skinned mesh renderer.
  - Adds `JointFollower` and related components to hook into XR Hands
  - Adds simple UI components built on HPUI
    - `HPUIInteractorLRVisual` - Use a linerender as a cursor showing where the interactor thinks the interaction is gooing to happe.
    - `HPUIInteractorTransformVisual` - Positions a transform as a cursor showing where the interactor thinks the interaction is gooing to happe.
