Put the real 3D digital human prefab here and name it Avatar.prefab.

Runtime load path:
Resources.Load<GameObject>("DigitalHuman/Avatar")

Recommended requirements:
- FBX/Prefab with SkinnedMeshRenderer.
- Humanoid rig is preferred, so the runtime can drive head, arms, and legs.
- Face the model toward the camera's -Z direction or adjust DigitalHumanAvatarView.externalAvatarLocalEuler.
