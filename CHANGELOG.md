# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
