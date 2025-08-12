# Safari Lake Simulation

## Overview
A C# multithreaded simulation with a real-time GUI built using Windows Forms.  
The simulation models three lakes with different capacities, where animals arrive, drink water, and leave according to specific rules.  
The project combines **concurrency control**, **object-oriented programming**, and a **dynamic graphical interface**.

## Features
- **Real-Time GUI**: Visualizes the lakes and the animals occupying each slot in real time.
- **Multiple Animal Types**:
  - **Flamingo**: Occupies 1 slot, average drinking time ~3.5s.
  - **Zebra**: Occupies 2 adjacent slots, average drinking time ~5.0s.
  - **Hippopotamus**: Occupies the entire lake, average drinking time ~5.0s.
- **Concurrency Management**:
  - Uses **Semaphore** to control available slots.
  - Uses **Mutex** to ensure safe access to lake data.
  - Special handling for hippopotamus exclusivity.
- **Randomized Behavior**:
  - Animal arrival and drinking times follow a Gaussian distribution.
- **OOP Design**:
  - Abstract `Animal` base class with derived types (`Flamingo`, `Zebra`, `Hippopotamus`).
  - `Lake` class handles placement logic, synchronization, and UI updates.
  - `AnimalFactory` creates animals and controls arrival times.

## Technologies Used
- **C# (.NET 6/7)**
- **Windows Forms**
- **Multithreading** (Thread, Semaphore, Mutex)
- **OOP Principles**
- **Gaussian Randomization** (Box-Muller Transform)

## How It Works
1. The simulation launches with 3 lakes:
   - Lake 1: 10 slots
   - Lake 2: 7 slots
   - Lake 3: 5 slots
2. Background threads spawn animals of each type at randomized intervals.
3. Each animal tries to occupy the required slots:
   - Flamingos may sit next to each other.
   - Zebras require 2 empty adjacent slots.
   - Hippopotamuses wait until the lake is empty and then occupy all slots.
4. The GUI updates in real time to show the animals entering and leaving.

## Screenshots
*(Add images here after running the simulation)*

## How to Run
1. Clone the repository:
   ```bash
   git clone https://github.com/<your-username>/Safari-Lake-Simulation.git
   ```
2. Open `Safari.sln` in **Visual Studio**.
3. Ensure the **Images** folder with `Flamingo.png`, `Zebra.png`, and `Hippo.png` is inside the project directory.
4. Build and run the project.

## License
This project is released under the MIT License.
