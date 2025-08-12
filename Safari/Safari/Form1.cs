using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace Safari
{
    public partial class Form1 : Form, ILakeUI
    {
        private Lake lake1, lake2, lake3;
        private List<Lake> lakes;
        private HashSet<Guid> viewedAnimalIds = new HashSet<Guid>();
        private Dictionary<Lake, PictureBox[]> lakeSlotsVisual = new Dictionary<Lake, PictureBox[]>();
        private readonly object viewedLock = new object();
        public PictureBox[] Lake1PictureBoxes;
        public PictureBox[] Lake2PictureBoxes;
        public PictureBox[] Lake3PictureBoxes;
        private volatile bool isClosing = false;

        /// <summary>
        /// Initializes the main GUI form, sets up lake picture boxes and event handlers.
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // Define UI slots for lake 1 (10 slots)
            Lake1PictureBoxes = new PictureBox[] {
                pictureBox_Lake1_0, pictureBox_Lake1_1, pictureBox_Lake1_2, pictureBox_Lake1_3, pictureBox_Lake1_4,
                pictureBox_Lake1_5, pictureBox_Lake1_6, pictureBox_Lake1_7, pictureBox_Lake1_8, pictureBox_Lake1_9 };

            // Define UI slots for lake 2 (7 slots)
            Lake2PictureBoxes = new PictureBox[] {
                pictureBox_Lake2_0, pictureBox_Lake2_1, pictureBox_Lake2_2, pictureBox_Lake2_3, pictureBox_Lake2_4,
                pictureBox_Lake2_5, pictureBox_Lake2_6 };

            // Define UI slots for lake 3 (5 slots)
            Lake3PictureBoxes = new PictureBox[] {
                pictureBox_Lake3_0, pictureBox_Lake3_1, pictureBox_Lake3_2, pictureBox_Lake3_3, pictureBox_Lake3_4 };

            // Register closing event to control simulation thread termination
            this.FormClosing += Form1_FormClosing;
        }

        /// <summary>
        /// Handles the event triggered when the form is about to close.
        /// Sets the flag to terminate background simulation threads safely.
        /// </summary>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
        }

        /// <summary>
        /// Safely removes an animal ID from the viewed list to allow it to be shown again.
        /// </summary>
        /// <param name="id">The animal's unique identifier to remove.</param>
        public void RemoveViewedAnimal(Guid id)
        {
            lock (viewedLock)
            {
                viewedAnimalIds.Remove(id);  // Remove animal ID from seen list
            }
        }

        /// <summary>
        /// Initializes the simulation on form load: creates lakes, maps them to UI,
        /// and launches the main simulation thread.
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            lake1 = new Lake(); lake1.Create(10); lake1.SetGUIForm(this);
            lake2 = new Lake(); lake2.Create(7); lake2.SetGUIForm(this);
            lake3 = new Lake(); lake3.Create(5); lake3.SetGUIForm(this);

            // Add lakes to list for random selection
            lakes = new List<Lake> { lake1, lake2, lake3 };

            // Map lake objects to their respective UI slots
            lakeSlotsVisual[lake1] = Lake1PictureBoxes;
            lakeSlotsVisual[lake2] = Lake2PictureBoxes;
            lakeSlotsVisual[lake3] = Lake3PictureBoxes;

            // Start simulation in background thread
            Thread simulationThread = new Thread(() => StartSimulation());
            simulationThread.IsBackground = true;
            simulationThread.Start();
        }

        /// <summary>
        /// Updates the visual state of a lake on the form.
        /// Ensures that each animal is only rendered in one slot (even if it occupies multiple).
        /// </summary>
        /// <param name="lake">The lake to update visually.</param>
        public void UpdateLakeUI(Lake lake)
        {
            // Clear animal tracking before refreshing
            lock (viewedLock)
            {
                viewedAnimalIds.Clear();
            }
            // Ensure we're running on UI thread
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLakeUI(lake)));
                return;
            }

            PictureBox[] boxes = null;

            // Retrieve the correct set of picture boxes for the lake
            if (lakeSlotsVisual.ContainsKey(lake))
                boxes = lakeSlotsVisual[lake];
            if (boxes == null) return;

            Animal[] animals = lake.GetAnimals();
            for (int i = 0; i < animals.Length; i++)
            {
                if (animals[i] == null)
                {
                    boxes[i].Image = null;   // Clear slot image
                    continue;
                }

                Guid id = animals[i].getId();

                // Ensure animal is only shown once
                lock (viewedLock)
                {
                    if (viewedAnimalIds.Contains(id))
                    {
                        boxes[i].Image = null; // Animal already displayed in another slot
                        continue;
                    }

                    viewedAnimalIds.Add(id);
                }

                // Determine the correct image for animal type
                string typeStr = animals[i].getType().ToString();
                string fileName = typeStr switch
                {
                    "f" => "Flamingo.png",
                    "z" => "Zebra.png",
                    "h" => "Hippo.png",
                    _ => null
                };

                // Load image and set it in the slot
                if (fileName != null)
                {
                    boxes[i].SizeMode = PictureBoxSizeMode.StretchImage;
                    boxes[i].Image = Image.FromFile(Path.Combine(Application.StartupPath, "Images", fileName));
                }
            }
        }

        /// <summary>
        /// Starts three background generators, one for each animal species.
        /// Each generator spawns animals at intervals based on species type.
        /// </summary>
        private void StartSimulation()
        {
            // Each animal type gets its own generator thread
            LaunchAnimalGenerator(() => new Flamingo(), 2.0);      // Flamingos arrive every ~2s
            LaunchAnimalGenerator(() => new Zebra(), 3.0);         // Zebras every ~3s
            LaunchAnimalGenerator(() => new Hippopotamus(), 10.0); // Hippos less frequently
        }

        /// <summary>
        /// Launches a dedicated background thread to spawn animals of a specific type
        /// at a randomized interval, and inserts them into one of the lakes.
        /// </summary>
        /// <param name="generateAnimal">Function to instantiate a new animal of the given type.</param>
        /// <param name="avgInterval">Average spawn interval (seconds) for the animal type.</param>
        private void LaunchAnimalGenerator(Func<Animal> generateAnimal, double avgInterval)
        {
            Thread generatorThread = new Thread(() =>
            {
                while (!isClosing)
                {
                    // Step 1: Simulate randomized arrival time (Gaussian)
                    Animal preview = generateAnimal();
                    preview.Create();    // Initialize preview animal for arrival distribution
                    double delaySeconds = AnimalFactory.GetArrivalTime(preview);
                    Thread.Sleep((int)(delaySeconds * 1000));

                    // Step 2: Generate a new animal instance
                    Animal newAnimal = generateAnimal();
                    newAnimal.Create(); // Initialize properties

                    // Step 3: Choose a random lake to assign the animal
                    Random rand = new Random(Guid.NewGuid().GetHashCode());
                    Lake lake = lakes[rand.Next(lakes.Count)];

                    // Step 4: Start a thread to manage the animal's lifecycle in the lake
                    Thread thread = new Thread(() => lake.Add(newAnimal));
                    thread.IsBackground = true;
                    thread.Start();
                }
            });

            generatorThread.IsBackground = true;
            generatorThread.Start();
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox15_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox_Lake1_9_Click(object sender, EventArgs e)
        {

        }
    }
}
