# Turn-based Strategy Game with Hierarchical PPO-based AI

A turn-based strategy game developed in Unity, featuring a hierarchical AI opponent. High-level strategic decisions (e.g., attack, defend, expand) are made by a **Proximal Policy Optimization (PPO)** agent trained using Unity ML-Agents. Low-level tactical execution is handled by deterministic rule-based systems.

## Features
- Hierarchical AI architecture
- PPO agent for macro-level decision making
- Rule-based system for micro-level execution
- Custom turn-based strategy environment
- Procedural level generation using Perlin noise with seed-based randomness for reproducible maps
- A\* pathfinding for unit movement and navigation
- BFS (Breadth-First Search) for checking building connections to the town center via roads

## Technologies
- **Unity 6000.2.4f1 (Unity 6)**
- **Unity ML-Agents 4.0.0**
- **C#** – game logic and agent integration
- **Python** – training the PPO agent (ML-Agents, PyTorch, TensorBoard)
- **Anaconda** – virtual environment for training

## How to Run the Game

### Option 1: Run in Unity Editor (recommended for development)
1. Clone this repository.
2. Open the project in **Unity 6000.2.4f1** (same version as above).
3. Open the main scene (path: `Assets/Scenes/MainScene.unity` – adjust if needed).
4. Press **Play** in the Unity Editor.

### Option 2: Run the built executable
A pre-built Windows executable is available in the `Builds/` folder. Download and run the `.exe` file. (No Unity installation required.)

> **Note:** The built executable may be large. If it exceeds GitHub's file size limits, it might not be included in this repository. In that case, please build the project locally using Option 1, or contact me for the executable.

## Training the PPO Agent (if needed)

The trained model is included in the project (in the `results/` folder). To re-train the agent from scratch:

1. **Set up the Python environment** (using Anaconda):
   ```bash
   conda create -n mlagents python=3.10
   conda activate mlagents
   pip install mlagents==4.0.0 torch tensorboard
2. **Run the training** (from the project root):
   ```bash
   mlagents-learn config/trainer_config.yaml --run-id=run1
3. Launch Unity, open the project, toggle the initializer script to training mode and press Play while the training script is running. The agent will start learning interactively.
4. Monitor training with TensorBoard:
   ```bash
   tensorboard --logdir results

The trained model files (.onnx) are saved in the results/ directory.

## Project structure ##

- Assets/Scripts/ - game logic and AI integration
- Assets/confing/ - trainer configuration for PPO
- results/ - trained models

## Author
Ádám Ördögh – MSc thesis project at Selye János University, Komárno, Slovakia
