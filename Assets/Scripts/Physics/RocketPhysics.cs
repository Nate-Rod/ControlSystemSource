using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//Matrix algebra
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

using System.Reflection;

/*
 * This class handles all of physics interactions between the rocket and its surrounding environment.
 * Player inputs, engine calculations, and UI-initiated control system actions are processed here.
 */
public class RocketPhysics : MonoBehaviour
{

    //Configurations


    //This class outlines the various engines that are attached to the rocket.
    [Serializable]
    public class Engines
    {
        public GameObject main;
        public float mainThrustMultiplier = 100f;
        public GameObject leftStabilizer;
        public GameObject rightStabilizer;
        public float stabilizerThrustMultiplier = 2f;
    }

    public Engines engines;


    //This class outlines the player controls used to activate each engine.
    [Serializable]
    public class Controls
    {
        public KeyCode MainThruster = KeyCode.Space;
        public KeyCode LeftThrust = KeyCode.Q;
        public KeyCode RightThrust = KeyCode.E;
        public KeyCode Reset = KeyCode.R;
    }
    public Controls controls;

    //This class implements hard limitations on the rocket's speed, angular speed, and 
    //defines a sensitivity zone for which the controller activates and disactivates.
    //This is largely to prevent cases in which the rocket can infinitely accelerate.
    [Serializable]
    public class PhysicsProperties
    {
        public float maxSpeed = 30f;
        public float maxAngularSpeed = 15f; //degrees per second
        public float controlThreshold = 5.0f; //degrees of "dead space" in controller
        public float velocityControlThreshold = 0.2f;
        public float maxSafeLandingSpeed = 10f;
    }
    public PhysicsProperties physicsProperties;

    //UI Configurations
    [Serializable]
    public class UIElements
    {
        public Text yPositionText;
        public Text yVelocityText;
        public Text angularPositionText;
        public Button observerControllerButton;
        public Button LQRControllerButton;
    }
    public UIElements uiElements;

    //Outlets
    Rigidbody2D _rb;
    Transform transform;

    //Tracking Variables
    bool missingParticleSystem = false;
    bool isSafeToLand = true;
    
    /*
     * This class creates a linear state space model in the form
     *          f(x, u) = Ax + Bu
     *          y = Cx + Du
     * where (A, B) are the dynamics of a state vector X
     * and (C, D) are the output matrices.
     * 
     * This implementation of a state-space model makes a few assumptions
     * which are NOT generalizable to every model.
     *      (1) The default constructor assumes a fully observable output.
     *      (2) The D matrix holds no bearing on the output. That is to say, D = 0.
     */
    private class LinearStateSpaceModel
    {
        public Matrix<double> A;
        public Matrix<double> B;
        public Matrix<double> C;
        public Matrix<double> D;

        //Assumes full observability and no D matrix.
        //Generates an empty set of matrices representing the state-space model.
        public LinearStateSpaceModel(int numberOfStates = 6, int numberOfInputs = 2)
        {
            A = CreateMatrix.Dense<double>(numberOfStates, numberOfStates);
            B = CreateMatrix.Dense<double>(numberOfStates, numberOfInputs);
            C = CreateMatrix.DenseIdentity<double>(numberOfStates);
            D = CreateMatrix.Dense<double>(0, 0);
        }
        
        //Also assumes no D matrix.
        public LinearStateSpaceModel(double[,] AMatrix, double[,] BMatrix,
                                     double[,] CMatrix)
        {
            A = CreateMatrix.DenseOfArray<double>(AMatrix);
            B = CreateMatrix.DenseOfArray<double>(BMatrix);
            C = CreateMatrix.DenseOfArray<double>(CMatrix);
            D = CreateMatrix.Dense<double>(0, 0);
        }
    }

    LinearStateSpaceModel stateSpaceModel = new LinearStateSpaceModel();
    LinearStateSpaceModel autonomousSS = new LinearStateSpaceModel();
    LinearStateSpaceModel LQRSS = new LinearStateSpaceModel();

    //Runs before any game input is processed, but after the script is initialized.
    //Use Awake() if you want to process tasks immediately upon initialization.
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        transform = GetComponent<Transform>();

        //Checking for proper configuration
        CheckForEngines();
        CheckUIConfiguration();

        //build state space models
        CreateLinearizedStateSpaceModel();
        CreateAutonomousStateSpaceModel();
        CreateLQRStateSpaceModel();
    }

    //Update runs once every frame. 
    //This should not be used with physics updates without the use of Time.deltaTime;
    void Update()
    {
        if(transform.position.y < -6)   //death condition
        {
            //do death stuff here
            ResetScene();
        }

        UpdateUI();
        HandleParticleSystems();
    }

    //Physics updates go here on a fixed timer. 
    //This allows you to omit the use of Time.deltaTime in your calcualtions,
    //but keeping Time.deltaTime will not negatively impact your performance or accuracy.
    void FixedUpdate()
    {
        HandlePlayerInput();
        ClampValues();
    }

    void LateUpdate()
    {
        CheckLandingSafety();
    }

    void CheckLandingSafety()
    {
        if (Mathf.Abs(_rb.velocity.y) <= physicsProperties.maxSafeLandingSpeed)
        {
            isSafeToLand = true;
        }
        else
        {
            isSafeToLand = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isSafeToLand)
        {
            print("Landed safely");
            //you did it! restart game?
        }
        else
        {
            print("You died");
            //explosion and reset scene
        }
    }

    //Applies physics limits to calculated values
    void ClampValues()
    {
        _rb.velocity = Vector2.ClampMagnitude(_rb.velocity, physicsProperties.maxSpeed);
        _rb.angularVelocity = Mathf.Clamp(_rb.angularVelocity, -physicsProperties.maxAngularSpeed, physicsProperties.maxAngularSpeed);
    }

    void UpdateUI()
    {
        float angularRotationInDegrees = GetAngularRotationInRadians() * Mathf.Rad2Deg;
        float height = _rb.position.y;
        float velocity = _rb.velocity.y;
        
        uiElements.yPositionText.text = "Height: " + Math.Round(height, 0) + "m";
        uiElements.angularPositionText.text = "Angle: " + Math.Round(GetAngularRotation(), 0) + " degrees";
        uiElements.yVelocityText.text = "Velocity: " + Math.Round(velocity, 0) + "m/s";
    }
   
    void HandlePlayerInput()
    {

        //Handles the thrust force for the main thrust key.
        if (Input.GetKey(controls.MainThruster))
        {
            //InverseTransformDirection() converts a vector in world-space to
            //the coordinates relative to our rocketship.
            Vector2 relativeUp = transform.InverseTransformDirection(Vector2.up);
            relativeUp = new Vector2(-relativeUp.x, relativeUp.y); //done to fix a miscalculation bug
            _rb.AddForceAtPosition(engines.mainThrustMultiplier * relativeUp * Time.deltaTime,
                engines.main.transform.position);
        }

        //Handles the thrust force for the left thrust key.
        if (Input.GetKey(controls.LeftThrust))
        {
            Vector2 relativeRight = transform.InverseTransformDirection(Vector2.right);
            Vector2 thrustForce = engines.stabilizerThrustMultiplier * relativeRight * Time.deltaTime;
            thrustForce.y = -thrustForce.y; //fixing a miscalculation bug
            _rb.AddForceAtPosition(thrustForce,
                engines.leftStabilizer.transform.position);
        }

        //Handles the thrust force for the right thrust key.
        if (Input.GetKey(controls.RightThrust))
        {
            Vector2 relativeLeft = transform.InverseTransformDirection(Vector2.left);
            Vector2 thrustForce = engines.stabilizerThrustMultiplier * relativeLeft * Time.deltaTime;
            thrustForce.y = -thrustForce.y; //fixing a miscalculation bug
            _rb.AddForceAtPosition(thrustForce,
                engines.rightStabilizer.transform.position);
        }

        //Handles actions for reset key.
        if (Input.GetKey(controls.Reset))
        {
            ResetScene();
        }

    }

    //Handles particle system effects. Runs on a stricter timer than the physics loop.
    void HandleParticleSystems()
    {
        if (!missingParticleSystem)
        {
            if (Input.GetKeyDown(controls.MainThruster))
            {
                engines.main.GetComponent<ParticleSystem>().Play();
            }
            if (Input.GetKeyUp(controls.MainThruster))
            {
                engines.main.GetComponent<ParticleSystem>().Stop();
            }
            if (Input.GetKeyDown(controls.LeftThrust))
            {
                engines.leftStabilizer.GetComponent<ParticleSystem>().Play();
            }
            if (Input.GetKeyUp(controls.LeftThrust))
            {
                engines.leftStabilizer.GetComponent<ParticleSystem>().Stop();
            }
            if (Input.GetKeyDown(controls.RightThrust))
            {
                engines.rightStabilizer.GetComponent<ParticleSystem>().Play();
            }
            if (Input.GetKeyUp(controls.RightThrust))
            {
                engines.rightStabilizer.GetComponent<ParticleSystem>().Stop();
            }
        }

    }

    //Checks to ensure engines and their dependencies are configured. 
    //If engines are not found, the script will abort and yell at you in the console.
    void CheckForEngines()
    {
        if (engines.main == null || engines.leftStabilizer == null || engines.rightStabilizer == null)
        {
            throw new Exception("One of the engines is not defined!\n"
                + "Check the 'Engine Properties' tab in the"
                + "'Rocket Physics' script and make sure all engines are defined.");
        }

        Vector3 mainEngineOffset = new Vector3(_rb.centerOfMass.x, 0, 0);
        engines.main.transform.Translate(mainEngineOffset);

        ParticleSystem[] particleSystems = new ParticleSystem[] {
            engines.main.GetComponent<ParticleSystem>(),
            engines.rightStabilizer.GetComponent<ParticleSystem>(),
            engines.leftStabilizer.GetComponent<ParticleSystem>()
        };

        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem == null) missingParticleSystem = true;
            else particleSystem.Stop();
        }
        if (missingParticleSystem)
        {
            print("Warning! At least one particle system is not connected. Visuals may be buggy.");
        }
    }

    void CreateLinearizedStateSpaceModel()
    {
        double[,] AMatrix = { {0, 1, 0, 0, 0, 0},
                              {0, 0, 0, 0, engines.mainThrustMultiplier/_rb.mass, 0}, 
                              {0, 0, 0, 1, 0, 0}, 
                              {0, 0, 0, 0, -engines.stabilizerThrustMultiplier/_rb.mass, 0}, 
                              {0, 0, 0, 0, 0, 1}, 
                              {0, 0, 0, 0, 0, 0} };
        double[,] BMatrix = { {0, 0},
                              {0, 1/_rb.mass}, 
                              {0, 0}, 
                              {1/_rb.mass, 0}, 
                              {0, 0}, 
                              {0, 1/_rb.inertia} };
        double[,] CMatrix = { {1, 0, 0, 0, 0, 0},
                              {0, 1, 0, 0, 0, 0},
                              {0, 0, 1, 0, 0, 0},
                              {0, 0, 0, 1, 0, 0},
                              {0, 0, 0, 0, 1, 0},
                              {0, 0, 0, 0, 0, 1}};
        stateSpaceModel = new LinearStateSpaceModel(AMatrix, BMatrix, CMatrix);
    }

    void CreateAutonomousStateSpaceModel()
    {
        double[,] AMatrix = { {0, 1, 0, 0, 0, 0},
                              {-0.0032, -0.3278, 0, 0, 639.7634, -35.8847},
                              {0, 0, 0, 1, 0, 0},
                              {0, 0, -1000, -110, 0, 0},
                              {0, 0, 0, 0, 0, 1},
                              {-0.01, -1.0111, 0, 0, -1111, -110.6822} };
        double[,] BMatrix = { {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0} };
        double[,] CMatrix = { {1, 0, 0, 0, 0, 0},
                              {0, 1, 0, 0, 0, 0},
                              {0, 0, 1, 0, 0, 0},
                              {0, 0, 0, 1, 0, 0},
                              {0, 0, 0, 0, 1, 0},
                              {0, 0, 0, 0, 0, 1}};
        autonomousSS = new LinearStateSpaceModel(AMatrix, BMatrix, CMatrix);
    }

    void CreateLQRStateSpaceModel()
    {
        //multiply A by 10 to get original
        double[,] AMatrix = { {0, .1, 0, 0, 0, 0},
                              {-.32, -3.18, 0, 0, -50.14, -3.53},
                              {0, 0, 0, .1, 0, 0},
                              {0, -.01, -.32, -.4, -2.95, 0},
                              {0, 0, 0, 0, 0, .1},
                              {-.98, -9.8, 0, 0, -463.08, -10.89}};
        double[,] BMatrix = { {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0},
                              {0, 0} };
        double[,] CMatrix = { {1, 0, 0, 0, 0, 0},
                              {0, 1, 0, 0, 0, 0},
                              {0, 0, 1, 0, 0, 0},
                              {0, 0, 0, 1, 0, 0},
                              {0, 0, 0, 0, 1, 0},
                              {0, 0, 0, 0, 0, 1}};
        LQRSS = new LinearStateSpaceModel(AMatrix, BMatrix, CMatrix);
    }

    //Calculates the next state vector normalized with respect to time.
    Vector<double> CalculateNextStates(LinearStateSpaceModel autonomousSS)
    {
        Vector<double> currentStates = CurrentStateVector();
        Vector<double> changeInStates = autonomousSS.A.Multiply(currentStates);
        //print("Change in states:\n" + changeInStates.ToString());
        return currentStates.Add(changeInStates * Time.deltaTime);
    }

    float GetAngularRotationInRadians()
    {
        //Vector2 relativeUp = transform.InverseTransformDirection(Vector2.up);
        //relativeUp = new Vector2(-relativeUp.x, relativeUp.y); //done to fix a miscalculation bug

        return Mathf.Deg2Rad * (GetAngularRotation());//Vector2.Angle(relativeUp, Vector2.up);
    }

    //Returns the angle (in degrees) clamped between -180, 180
    float GetAngularRotation()
    {
        float clampedRotation = _rb.rotation % 360;
        if(clampedRotation <= -180) return clampedRotation += 360;
        else if (clampedRotation > 180) return clampedRotation -= 360;
        return clampedRotation;
    }

    Vector<double> CurrentStateVector()
    {
        float angularRotation = GetAngularRotationInRadians();
        double[] newStateArray = {_rb.position.x, _rb.velocity.x,
                                  _rb.position.y, _rb.velocity.y,
                                  angularRotation, _rb.angularVelocity};
        return CreateVector.DenseOfArray<double>(newStateArray);
    }
    //Right now, this just instantaneously "snaps" the rocket back to 0.0 degrees. 
    //We want this to gradually turn the rocket to 0.0 degrees by applying engine thrusts appropriately.
    void SetRotation(float angle = 0.0f)
    {
        print("Setting rotation to " + angle + " degrees. . . ");
        PrettyPrintStateSpaceModel(stateSpaceModel);
        //LinearStateSpaceModel autonomousSS = CreateAutonomousStateSpaceModel();
        Vector<double> newStates = CalculateNextStates(autonomousSS);
        //States newStates = CalculateNextStates(autonomousSS);
        print("New State (after 1 time step)\n" + newStates.ToString());
        _rb.SetRotation(angle);
        _rb.angularVelocity = 0;
    }

    void ActivateObserverController()
    {
        //print("Activated observer controller at state\n" + CurrentStateVector().At(4).ToString());
        StartCoroutine("ApplyObserverControllerPhysics");
    }

    void ActivateLQRControllerPhysics()
    {
        print("Activated LQR controller at state\n" + CurrentStateVector().ToString());
        StartCoroutine("ApplyLQRControllerPhysics");
    }

    IEnumerator ApplyObserverControllerPhysics()
    {
        //LinearStateSpaceModel autonomousSS = CreateAutonomousStateSpaceModel();
        Vector<double> newStates = CalculateNextStates(autonomousSS);
        _rb.SetRotation((float)(newStates.At(4) * Mathf.Rad2Deg));

        //Clamp angular velocity to avoid rapid jerking corrective motion
        _rb.angularVelocity = Mathf.Clamp(
            (float)newStates.At(5), 
            -physicsProperties.maxAngularSpeed,
             physicsProperties.maxAngularSpeed);

        //print(newStates.At(4).ToString());
        if(Mathf.Abs(_rb.rotation) <= physicsProperties.controlThreshold)
        {
            StopCoroutine("ApplyObserverControllerPhysics");
        }
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        //yield return new WaitForEndOfFrame();
        StartCoroutine("ApplyObserverControllerPhysics");
    }

    IEnumerator ApplyLQRControllerPhysics()
    {
        Vector<double> newStates = CalculateNextStates(LQRSS);
        _rb.SetRotation((float)(newStates.At(4)* Mathf.Rad2Deg));

        //Clamp angular velocity to avoid rapid jerking corrective motion
        //_rb.angularVelocity = Mathf.Clamp(
        //   (float)newStates.At(5),
        //   -physicsProperties.maxAngularSpeed,
        //   physicsProperties.maxAngularSpeed);
        //if(_rb.rotation < 0) _rb.angularVelocity = -(float)newStates.At(5);
        //else 
        _rb.angularVelocity = (float)newStates.At(5);
        //print(newStates.At(4).ToString());
        print(_rb.angularVelocity);

        //if (_rb.rotation < 0) _rb.angularVelocity = -_rb.angularVelocity;
        //print(Mathf.Abs(_rb.angularVelocity) <= physicsProperties.velocityControlThreshold);

        if (Mathf.Abs(_rb.rotation) <= physicsProperties.controlThreshold 
            || Mathf.Abs(_rb.angularVelocity) <= physicsProperties.velocityControlThreshold)
        {
            print("Coroutine should stop here...");
            StopCoroutine("ApplyLQRControllerPhysics");
            _rb.angularVelocity = 0;
        }
        yield return new WaitForSeconds(Time.fixedDeltaTime);
        //yield return new WaitForEndOfFrame();
        StartCoroutine("ApplyLQRControllerPhysics");
    }

    //Checks to see if all UI elements are properly configured.
    void CheckUIConfiguration()
    {
        bool isAutomaticMode = false;
        uiElements.observerControllerButton.onClick.AddListener(
            delegate {
                //SetRotation();
                //if (!isAutomaticMode)
                //{
                    ActivateObserverController();
                    
                /*
                    isAutomaticMode = !isAutomaticMode;
                    Text buttonText = uiElements.observerControllerButton.GetComponentInChildren<Text>();
                    if (isAutomaticMode) buttonText.text = "Observer: ON";
                    else buttonText.text = "Observer: OFF";
                */
                //}
            }
        );

        uiElements.LQRControllerButton.onClick.AddListener(
            delegate
            {
                //if (!isAutomaticMode)
                //{
                    ActivateLQRControllerPhysics();
                /*
                    isAutomaticMode = !isAutomaticMode;
                    Text buttonText = uiElements.LQRControllerButton.GetComponentInChildren<Text>();
                    if (isAutomaticMode) buttonText.text = "LQR: ON";
                    else buttonText.text = "LQR: OFF";
                */
                //}
                
            }
        );

        
    }
    void PrettyPrintStateSpaceModel(LinearStateSpaceModel ss = null)
    {
        if (ss == null) {
            print("The PrettyPrintStateSpaceModel() method was called, but the state space model wasn't defined.");
        }
        else
        {
            print("-------------------\nLinear State Space Model:");
            print("A:\n" + ss.A.ToString());
            print("B:\n" + ss.B.ToString());
            print("C:\n" + ss.C.ToString());
            print("D:\n" + ss.D.ToString());
            print("Current state:\n" + CurrentStateVector().ToString());
            print("-------------------");
        }

        
    }

    void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
