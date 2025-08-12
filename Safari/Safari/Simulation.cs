using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Safari
{
    /// <summary>
    /// Interface for UI components that need to update when a lake's state changes.
    /// </summary>
    public interface ILakeUI
    {
        /// <summary>
        /// Updates the visual representation of the given lake.
        /// </summary>
        /// <param name="lake">The lake object to reflect in the UI.</param>
        void UpdateLakeUI(Lake lake);
    }

    /// <summary>
    /// Class responsible for calculating the drinking time of animals.
    /// </summary>
    class Calculate_time
    {
        /// <summary>
        /// Returns the drinking time for the given animal.
        /// </summary>
        /// <param name="animal">The animal whose drinking time is being calculated.</param>
        /// <returns>Drinking time as a double (currently placeholder value 0).</returns>
        double DrinkTime(Animal animal) { return 0; }  // Placeholder implementation.
    }

    /// <summary>
    /// Abstract base class representing a general animal.
    /// </summary>
    public abstract class Animal
    {
        public char type;      // Character representing the type of the animal ('f', 'z', 'h', etc.)
        public int slot_size;   // Number of slots the animal occupies in the lake.
        public double mean_drink;    // Average drinking time for the animal.
        public Guid id;              // Unique identifier for the animal instance.


        /// <summary>
        /// Abstract method to initialize animal properties; must be implemented by derived classes.
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// Returns the type character of the animal.
        /// </summary>
        /// <returns>Type of the animal as char.</returns>
        public char getType() => this.type;

        /// <summary>
        /// Returns the unique identifier of the animal.
        /// </summary>
        /// <returns>GUID of the animal.</returns>
        public Guid getId() => this.id;
    }

    /// <summary>
    /// Represents a Flamingo, a type of animal that occupies 1 slot and drinks for 3.5 seconds.
    /// </summary>
    class Flamingo : Animal
    {
        /// <summary>
        /// Initializes a Flamingo with specific properties.
        /// </summary>
        public override void Create()
        {
            this.slot_size = 1;            // Flamingo takes up 1 slot in the lake.
            this.mean_drink = 3.5;          // Average drinking time is 3.5 seconds.
            this.type = 'f';               // Represented by character 'f'.
            this.id = Guid.NewGuid();      // Assign a new unique identifier.
        }
    }


    /// <summary>
    /// Represents a Zebra, a type of animal that occupies 2 slots and drinks for 5.0 seconds.
    /// </summary>
    class Zebra : Animal
    {
        /// <summary>
        /// Initializes a Zebra with specific properties.
        /// </summary>
        public override void Create()
        {
            this.slot_size = 2;           // Zebra takes up 2 slots in the lake.
            this.mean_drink = 5.0;        // Average drinking time is 5.0 seconds.
            this.type = 'z';             // Represented by character 'z'.
            this.id = Guid.NewGuid();     // Assign a new unique identifier.
        }
    }



    /// <summary>
    /// Represents a Hippopotamus, a type of animal that drinks for 5.0 seconds.
    /// (Note: slot size not assigned here, may be handled elsewhere.)
    /// </summary>
    class Hippopotamus : Animal
    {
        /// <summary>
        /// Initializes a Hippopotamus with specific properties.
        /// </summary>
        public override void Create()
        {
            this.mean_drink = 5.0;        // Average drinking time is 5.0 seconds.
            this.type = 'h';             // Represented by character 'h'.
            this.id = Guid.NewGuid();    // Assign a new unique identifier.
        }
    }




    public class Lake
    {
        protected int slots;
        protected Semaphore semaphore_slots;
        public Animal[] animals;
        protected Mutex lock_check;
        private bool hippoWaiting = false;
        private int activeAnimals = 0;
        private ILakeUI guiForm;

        /// <summary>
        /// Returns the current state of the animal array in the lake.
        /// </summary>
        /// <returns>An array of Animal objects representing the lake's slots.</returns>
        public Animal[] GetAnimals()
        {
            return animals;
        }
        /// <summary>
        /// Initializes the lake with a specific number of slots and necessary synchronization objects.
        /// </summary>
        /// <param name="num_slots">Number of available drinking slots in the lake.</param>
        public void Create(int num_slots)
        {
            this.slots = num_slots;
            this.animals = new Animal[num_slots];    // Create an array to hold animals.
            for (int i = 0; i < num_slots; i++)
            {
                this.animals[i] = null;    // Initialize all slots to empty.
            }
            this.semaphore_slots = new Semaphore(num_slots, num_slots);  // Semaphore controls available slots.
            this.lock_check = new Mutex();     // Mutex to ensure mutual exclusion for lake access.   
        }

        /// <summary>
        /// Links a GUI form implementing ILakeUI to enable visual updates.
        /// </summary>
        /// <param name="form">Form implementing ILakeUI interface.</param>
        public void SetGUIForm(ILakeUI form)
        {
            this.guiForm = form;
        }


        /// <summary>
        /// Generates a random value using a normal distribution (Gaussian).
        /// </summary>
        /// <param name="mean">Mean of the distribution.</param>
        /// <param name="stdDev">Standard deviation of the distribution.</param>
        /// <returns>A random value sampled from the specified normal distribution.</returns>
        public static double NextGaussian(double mean, double stdDev)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());  // Create seed for randomness.
            double u1 = 1.0 - rand.NextDouble();  // Uniform(0,1] random double
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);  // Box-Muller transform
            return mean + stdDev * randStdNormal;    // Scale and shift to desired mean and stddev
        }

        /// <summary>
        /// Attempts to place an animal in the lake according to its placement rules.
        /// </summary>
        /// <param name="animal">The animal to be placed.</param>
        /// <returns>True if placement was successful, false otherwise.</returns>
        public bool check_if_have_place(Animal animal)
        {
            if (animal.type == 'f')
            {
                bool found_flamingo = false;
                // Check if a flamingo is already in the lake
                for (int i = 0; i < slots; i++)
                {
                    if (this.animals[i] != null && this.animals[i].type == 'f')
                    {
                        found_flamingo = true;
                        break;
                    }
                }

                if (!found_flamingo)
                {
                    // Place flamingo in first available empty slot
                    for (int i = 0; i < slots; i++)
                    {
                        if (this.animals[i] == null)
                        {
                            this.animals[i] = animal;
                            return true;
                        }
                    }
                }
                else
                {
                    // Try to place the flamingo next to another flamingo
                    for (int i = 0; i < slots; i++)
                    {
                        if (this.animals[i] != null && this.animals[i].type == 'f')
                        {
                            if (i - 1 >= 0 && this.animals[i - 1] == null)
                            {
                                this.animals[i - 1] = animal;
                                return true;
                            }
                            if (i + 1 < slots && this.animals[i + 1] == null)
                            {
                                this.animals[i + 1] = animal;
                                return true;
                            }
                        }
                    }
                }
            }
            else if (animal.type == 'z')
            {
                // Look for two adjacent empty slots for zebra
                for (int i = 0; i < slots - 1; i++)
                {
                    if (this.animals[i] == null && this.animals[i + 1] == null)
                    {
                        this.animals[i] = animal;
                        this.animals[i + 1] = animal;
                        return true;
                    }
                }
            }
            return false;   // No suitable placement found
        }

        /// <summary>
        /// Adds an animal to the lake and simulates its drinking behavior.
        /// Includes logic for synchronization, slot acquisition, and hippopotamus exclusivity.
        /// </summary>
        /// <param name="animal">The animal to be added and simulated.</param>
        public void Add(Animal animal)
        {
            int threadId = Thread.CurrentThread.ManagedThreadId;
            if (animal.type != 'h')   // Not a hippopotamus
            {
                bool acquired = false;
                bool entered = false;

                while (!entered)
                {
                    if (hippoWaiting)
                    {
                        Thread.Sleep(10);  // Wait if a hippo is waiting
                        continue;
                    }

                    // Try to acquire the required number of slots
                    for (int i = 0; i < animal.slot_size; i++)
                    {
                        semaphore_slots.WaitOne();  
                    }
                    acquired = true;

                    lock_check.WaitOne();   // Lock critical section
                    if (hippoWaiting)
                    {
                        // A hippo has arrived while trying to enter
                        lock_check.ReleaseMutex();
                        semaphore_slots.Release(animal.slot_size);
                        acquired = false;
                        Thread.Sleep(10);
                        continue;
                    }

                    // Try to place the animal in the lake
                    entered = check_if_have_place(animal);
                    if (entered)
                    {
                        activeAnimals++;   // Update count of active animals
                    }
                    else
                    {
                        // Release slots if placement failed
                        semaphore_slots.Release(animal.slot_size);
                        acquired = false;
                        lock_check.ReleaseMutex();
                        Thread.Sleep(10);
                        continue; 
                    }

                    lock_check.ReleaseMutex();  // Unlock critical section
                }

                guiForm?.UpdateLakeUI(this);   // Update UI after placement

                // Simulate drinking time
                double stdDev = animal.mean_drink * 0.1;
                double sleepTime = Math.Max(0.1, NextGaussian(animal.mean_drink, stdDev));
                Thread.Sleep((int)(sleepTime * 1000));

                lock_check.WaitOne();
                int toRemove = animal.slot_size;

                // Remove animal from its slots
                for (int i = 0; i < slots && toRemove > 0; i++)
                {
                    if (animals[i] != null && animals[i].id == animal.id)
                    {
                        animals[i] = null;
                        toRemove--;
                    }
                }
                activeAnimals--;  // Decrease count of active animals
                lock_check.ReleaseMutex();

                guiForm?.UpdateLakeUI(this);  // Update UI after removal

                if (acquired)
                {
                    semaphore_slots.Release(animal.slot_size);  // Release slots
                }
            }
            else // Hippopotamus logic
            {

                while (hippoWaiting)
                {
                    Thread.Sleep(10);  // Wait for other hippo to finish
                }

                hippoWaiting = true;
                lock_check.WaitOne();

                // Wait until no other animals are drinking
                while (activeAnimals > 0)
                {
                    lock_check.ReleaseMutex();
                    Thread.Sleep(10);
                    lock_check.WaitOne();
                }

                // Occupy all lake slots
                for (int i = 0; i < slots; i++)
                {
                    animals[i] = animal;
                }

                guiForm?.UpdateLakeUI(this);   // Update UI with hippo

                // Simulate drinking time
                double stdDev = animal.mean_drink * 0.1;
                double sleepTime = Math.Max(0.1, NextGaussian(animal.mean_drink, stdDev));
                Thread.Sleep((int)(sleepTime * 1000));

                // Clear all slots
                for (int i = 0; i < slots; i++)
                {
                    animals[i] = null;
                }

                guiForm?.UpdateLakeUI(this);   // Update UI after hippo leaves
                lock_check.ReleaseMutex();
                hippoWaiting = false;

            }
        }

    }


    class AnimalFactory
    {

        /// <summary>
        /// Creates a new randomly selected animal (Flamingo, Zebra, or Hippopotamus).
        /// </summary>
        /// <returns>A newly created Animal instance.</returns>
        public static Animal CreateRandomAnimal()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            int choice = rnd.Next(3);  // Random integer from 0 to 2

            Animal a;
            if (choice == 0)
                a = new Flamingo();
            else if (choice == 1)
                a = new Zebra();
            else
                a = new Hippopotamus();

            a.Create();  // Initialize properties
            return a;
        }


        /// <summary>
        /// Calculates the random arrival time for an animal based on its type.
        /// </summary>
        /// <param name="a">The animal whose arrival time to compute.</param>
        /// <returns>Randomized arrival time based on Gaussian distribution.</returns>
        public static double GetArrivalTime(Animal a)
        {
            double mean = 0;
            if (a.type == 'f') mean = 2.0;
            else if (a.type == 'z') mean = 3.0;
            else if (a.type == 'h') mean = 10.0;

            double std = mean * 0.1;
            return Math.Max(0.1, Lake.NextGaussian(mean, std));
        }
    }
}
