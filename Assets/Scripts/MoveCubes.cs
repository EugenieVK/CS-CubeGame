using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Text;
using System;
using NetworkAPI;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;

public class MoveCubes : MonoBehaviour
{
    public Camera mainCamera;
    public Camera itCamera;

    public GameObject localCube;
    //public Vector3 localCubePos = new();
    public TextMeshProUGUI countdownText;

    public List<string> cubes = new();
    public Dictionary<string, GameObject> remoteCubes = new();
    public Dictionary<string, Vector3> remoteCubePos = new();   

    //public Messaging messaging = new Messaging();
    //public WebsocketServerCon server = new WebsocketServerCon();
    private readonly SignalRConnector _connector = new SignalRConnector();

    private Rigidbody _rb;

    private Vector3 moveDirection;

    private string cubeId;
    private bool isIt = true;
    private bool inputEnabled = true;

    public float normalSpeed;
    public float itSpeed;
    private float speed;
    public float rotationSpeed;
    private Quaternion targetRotation;
    private bool isRotating = false;

    private float serverUpdateInterval = 0.05f;
    private float updateTimer = 0f;

    void Awake() { }
    void OnEnable() { }
    async void Start()
    {

        countdownText.gameObject.SetActive(false);
        localCube = GameObject.Find("CubeA");
        _rb = localCube.GetComponent<Rigidbody>();
        targetRotation = _rb.rotation;

        cubeId = await _connector.Init("http://localhost:80/hub");
        isIt = _connector.connectionCount <= 1;

        speed = normalSpeed;
        if (isIt)
        {
            speed = itSpeed;
            if (localCube.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = Color.red;
            }
        }

        _rb.constraints = _rb.constraints | RigidbodyConstraints.FreezePositionY;

        SwitchCameras();
    }

    // Update is called once per frame
    void Update()
    {
        moveDirection = Vector3.zero;
        if (inputEnabled)
        {
            if (Input.anyKey)
            {
                if (isIt)
                {
                    if (!isRotating)
                    {
                        if (Input.GetKeyDown(KeyCode.RightArrow))
                        {
                            RotateCube(90);
                        }
                        if (Input.GetKeyDown(KeyCode.LeftArrow))
                        {
                            RotateCube(-90);
                        }
                    }
                }
                else
                {
                    if (Input.GetKey(KeyCode.RightArrow))
                    {
                        moveDirection += localCube.transform.right;
                    }
                    if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        moveDirection -= localCube.transform.right;
                    }
                }
  
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    moveDirection += localCube.transform.forward;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    moveDirection -= localCube.transform.forward;
                }

            }
        

        }

        if (isIt)
        {
            speed = itSpeed;
        } else
        {
            speed = normalSpeed;
        }

        _connector.Msg += new SignalRConnector.MsgHandler(HandleMessage);
        SwitchCameras();
    }

    async void OnDisable() {
        await _connector.SendMessageAsync($"{cubeId}:DISCONNECT:NA:NA", "DISCONNECT", cubeId);
        await _connector.CloseConnection();
        Debug.Log("OnDisable Called"); 
    }

    void OnCollisionEnter(Collision collision)
    {

        string nameOfCollision = collision.gameObject.name;
        if (nameOfCollision != "Floor" && nameOfCollision != "Wall")
        {
            if (isIt)
            {
                string itString = isIt ? "IT" : "NOT";
                string msgType = "COLLISION";
                _ = _connector.SendMessageAsync($"{cubeId}:{msgType}:{itString}:na", msgType, collision.gameObject.name);
                StopBeingIt();
            }
        }
    }

    void FixedUpdate()
    {
        updateTimer += Time.deltaTime;
        if (_rb)
        {
            if (moveDirection != Vector3.zero)
            {
                _rb.linearVelocity = moveDirection.normalized * speed;

                string itString = isIt ? "IT" : "NOT";
                string msgType = "MOVE";

                if (updateTimer >= serverUpdateInterval)
                {
                    _ = _connector.SendMessageAsync($"{cubeId}:{msgType}:{itString}:x={_rb.position.x},y={_rb.position.y},z={_rb.position.z}", msgType, cubeId);
                    updateTimer = 0f;
                }
                
            }
            else
            {
                _rb.linearVelocity = Vector3.zero;
            }

            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

            if (Quaternion.Angle(_rb.rotation, targetRotation) < 0.1f)
            {
                _rb.MoveRotation(targetRotation);
                isRotating = false;
            }
        }
        
    }

    void RotateCube(float rotation)
    {
        isRotating = true;
        targetRotation *= Quaternion.Euler(0, rotation, 0);
    }



    public void moveRemoteCube(string id, string itStatus, string coords)
    {
        Vector3 pos = GetPositionFromString(coords);
        GameObject cube = remoteCubes[id];
        cube.transform.position = pos;
        remoteCubePos[id] = pos;
        changeRemoteCubeColor(cube, itStatus);
    }

    public void changeRemoteCubeColor(GameObject cube, string status)
    {
        if (cube.TryGetComponent<Renderer>(out var renderer))
        {
            if(status == "IT")
            {
                renderer.material.color = Color.red;
            } else
            {
                renderer.material.color = Color.white;
            }
            
        }
    }

    public Vector3 GetPositionFromString(string coords)
    {
        float x = 1.0f, y = 1.0f, z = 1.0f;
        string[] xyz = coords.Split(',');
        foreach (string coord in xyz)
        {
            string[] loc = coord.Split('=');
            switch (loc[0])
            {
                case "x":
                    x = float.Parse(loc[1], CultureInfo.InvariantCulture.NumberFormat); break;
                case "y":
                    y = float.Parse(loc[1], CultureInfo.InvariantCulture.NumberFormat); break;
                case "z":
                    z = float.Parse(loc[1], CultureInfo.InvariantCulture.NumberFormat); break;
            }
        }

        Vector3 pos = new Vector3(x, y, z);
        return pos;
    }

    public void HandleMessage(string msg)
    {
        string[] msgParts = msg.Split(":");
        string id = msgParts[0];
        string type = msgParts[1];
        string itStatus = msgParts[2];
        string coords = msgParts[3];


        if(type == "MOVE")
        {
            if (cubes.Contains(id))
            {
                moveRemoteCube(id, itStatus, coords);
            }
            else
            {
                AddNewCube(id, itStatus, coords);
            }
        } else if(type == "COLLISION")
        {
            MakeIt();
        } else if(type == "DISCONNECT")
        {
            RemoveCube(id);
        }
        
    }
    public void AddNewCube(string id, string itStatus, string coords) {
        cubes.Add(id);
        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        newCube.name = id;

        newCube.AddComponent<Rigidbody>();
        Rigidbody newCubeRb = newCube.GetComponent<Rigidbody>();
        newCubeRb.isKinematic = true;

        Vector3 pos = GetPositionFromString(coords);
        newCube.transform.position = pos;
        remoteCubePos[id] = pos;

        remoteCubes[id] = newCube;
    }

    public void RemoveCube(string id)
    {
        cubes.Remove(id);
        Destroy(remoteCubes[id]);
        remoteCubes.Remove(id);
        remoteCubePos.Remove(id);

    }

    public void MakeIt()
    {
        isIt = true;
        if (localCube.TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = Color.red;
        }
        StartCoroutine(DisableInput());

    }

    public IEnumerator DisableInput()
    {
        inputEnabled = false;
        countdownText.gameObject.SetActive(true);
        countdownText.fontSize = 80;

        float time = 5;
        float remainingTime = time;
        while (remainingTime > 0)
        {
            countdownText.text = remainingTime.ToString();
            countdownText.transform.localScale = Vector3.one * (1 + (1 - remainingTime / time) * 0.5f);

            yield return new WaitForSeconds(1);
            remainingTime--;
        }

        countdownText.fontSize = 30;
        countdownText.text = "YOU'RE IT";
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);
        inputEnabled = true;
    }

    public void StopBeingIt()
    {
        isIt = false;
        if (localCube.TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = Color.white;
        }
    }

    void SwitchCameras()
    {
        mainCamera.enabled = !isIt;
        itCamera.enabled = isIt;
    }
}