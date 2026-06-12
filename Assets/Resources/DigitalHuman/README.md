# Digital Human Avatar Setup Guide

## Quick Setup (3 steps)

### Step 1: Download a free character from Unity Asset Store
Recommended characters (search in Unity → Window → Asset Store):
- **SD Unity-Chan** (Q版, child-friendly, humanoid, best for autistic children)
- **Quirky Series Free Animals** (cute cartoon style)
- **Meshtint Free Poly Yei** (simple polygon style)

### Step 2: Import and configure
1. Import the character into your project
2. Select the FBX/model file in Project window
3. In Inspector → Rig tab: set Animation Type to **Humanoid**, click **Apply**
4. Click **Configure** to verify bone mapping, then **Done**

### Step 3: Create prefab and animations
1. Drag the character into a Scene
2. Go to menu: **Tools → Digital Human → Create Animator Controller**
3. Go to menu: **Tools → Digital Human → Setup Avatar Prefab** (with character selected)
4. In the Animator window, assign an AnimationClip to each state:
   - Idle, Greeting, Speaking, OfferItem, ColorPrompt
   - ImitationWave, ImitationClap, ImitationNod, Celebrate
5. Move the prefab to: **Assets/Resources/DigitalHuman/Avatar.prefab**

## Animation Clip Requirements
- **Looping animations**: Idle, Speaking (enable Loop Time in clip settings)
- **One-shot animations**: Greeting, OfferItem, ColorPrompt, ImitationWave, ImitationClap, ImitationNod, Celebrate
- All clips must be Humanoid animation type

## How it works
The Animator Controller uses Trigger parameters matching the pose enum names.
When the game requests a pose (e.g., "Greeting"), the code calls SetTrigger("Greeting"),
and the Animator transitions from Any State to the matching animation state.
After the animation finishes (one-shot) or immediately (looping), it transitions as configured.
