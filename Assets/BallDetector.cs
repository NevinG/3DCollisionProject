using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallDetector : MonoBehaviour
{
    // Start is called before the first frame update
    WebCamTexture webcam;
    public MeshRenderer meshRenderer;
    Color32 ballColor = Color.yellow;
    public Image ballColorImage;
    public Image detectedColorImage;
    public TextMeshProUGUI ballColorText;
    public TextMeshProUGUI detectedColorText;
    public TextMeshProUGUI ballPositionText;
    public TextMeshProUGUI ballDistanceText;
    public TextMeshProUGUI wallDistanceText;
    public MeshRenderer processedImage;
    public SpriteRenderer debugBallRenderer;
    Color32[] pixels;
    Texture2D tex;
    float yScale = 9;
    float xScale;
    float colorSensitivity = 10;
    bool ballDectedOnLastFrame = false;
    int lastCenterPixel;
    int ballAtWallSize = 200;
    BallData currentBallData;
    public GameObject cube;
    public GameObject collidingBall;

    float timer = 0;
    bool countingDown = false;

    float hitWallTimer = 0;
    
    void Start()
    {
        //settup webacam
        webcam = new WebCamTexture();
        webcam.Play();
        meshRenderer.material.mainTexture = webcam;
        
        //set quads aspect ratio to match webcam
        
        float aspectRatio = (float)webcam.width / webcam.height;
        xScale = aspectRatio * yScale;
        meshRenderer.transform.localScale = new Vector3(xScale, yScale, 1);
        processedImage.transform.localScale = new Vector3(xScale, yScale, 1);

        //processed image
        pixels = webcam.GetPixels32();
        tex = new Texture2D(webcam.width, webcam.height);
        for(int i = 0; i < tex.width; i++)
        {
            for(int j = 0; j < tex.height; j++)
            {
                tex.SetPixel(i, j, Color.clear);
            }
        }

        tex.Apply();
        processedImage.material.mainTexture = tex;

        //debug settup
        Debug.Log("Webcam Capture: " + webcam.width + "x" + webcam.height);
        ballColorImage.color = ballColor;
        ballColorText.text = "(" + (ballColor.r + ", " + ballColor.g + ", " + ballColor.b) + ")";

        //generate cubes
        GenerateCubeWall();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            //get ball color
            ballColor = webcam.GetPixel(webcam.width / 2, webcam.height / 2);
            ballColorImage.color = ballColor;
            ballColorText.text = "(" + (ballColor.r + ", " + ballColor.g + ", " + ballColor.b) + ")";
        }

        //get pixels
        pixels = webcam.GetPixels32();

        //search for ball
        bool foundBall = false;
        if(ballDectedOnLastFrame) //search from center pos of the last ball
        {
            if (SimilarColors(pixels[lastCenterPixel], ballColor))
            {
                foundBall = true;
                detectedColorImage.color = pixels[lastCenterPixel];
                detectedColorText.text = "(" + (pixels[lastCenterPixel].r + ", " + pixels[lastCenterPixel].g + ", " + pixels[lastCenterPixel].b) + ")";
                currentBallData = countPixels(lastCenterPixel);
                ballPositionText.text = "Ball Position: " + pixelToWorldPosition(currentBallData.centerPos);
                ballDistanceText.text = "Ball Distance: " + currentBallData.area + " pixels";

                debugBallRenderer.transform.position = pixelToWorldPosition(currentBallData.centerPos);
            }
        }
        else //serach entire screen
        {
            for (int i = 0; i < pixels.Length; i += 153)
            {
                if (SimilarColors(pixels[i], ballColor))
                {
                    foundBall = true;
                    detectedColorImage.color = pixels[i];
                    detectedColorText.text = "(" + (pixels[i].r + ", " + pixels[i].g + ", " + pixels[i].b) + ")";
                    currentBallData = countPixels(i);
                    ballPositionText.text = "Ball Position: " + pixelToWorldPosition(currentBallData.centerPos);
                    ballDistanceText.text = "Ball Distance: " + currentBallData.area + " pixels";

                    debugBallRenderer.transform.position = pixelToWorldPosition(currentBallData.centerPos);
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            countingDown = true;
        }

        if(countingDown)
        {
            timer += Time.deltaTime;
            if(timer >= 5 && foundBall)
            {
                countingDown = false;
                ballAtWallSize = currentBallData.area;
                wallDistanceText.text = "Wall Distance: " + ballAtWallSize + " pixels";
            }
        }

        ballDectedOnLastFrame = foundBall;

        hitWallTimer += Time.deltaTime;

        if(hitWallTimer >= 5 && foundBall && currentBallData != null && currentBallData.area - ballAtWallSize < 10)
        {
            hitWallTimer = 0;
            Debug.Log("HIT THE WALL at: " + pixelToWorldPosition(currentBallData.centerPos));
            GameObject g =Instantiate(collidingBall, pixelToWorldPosition(currentBallData.centerPos), Quaternion.identity);
            g.GetComponent<Rigidbody>().AddForce(new Vector3(0, 0, 1000));
        }
    }

    BallData countPixels(int i)
    {
        Vector2Int centerPos = Vector2Int.zero;
        int onRight = 0;
        int onLeft = 0;
        int onTop = 0;
        int onBottom = 0;

        //check how many pixels on top are similar
        int j = webcam.width;
        bool counting = true;
        int[] countingDistances = {10, 9, 8, 5, 2, 1};
        while (counting)
        {
            counting = false;
            for(int index = 0; index < countingDistances.Length; index++)
            {
                int searchIndex = i + (onTop * j) + (countingDistances[index] * j);
                if (searchIndex >= 0 && searchIndex < pixels.Length && SimilarColors(pixels[searchIndex],ballColor, 2f))
                {
                    counting = true;
                    onTop += countingDistances[index];
                    break;
                }
            }
        }

        //calculate how many on Bottom are similar
        counting = true;
        j = webcam.width;
        while (counting)
        {
            counting = false;
            for (int index = 0; index < countingDistances.Length; index++)
            {
                int searchIndex = i - (onBottom * j) - (countingDistances[index] * j);
                if (searchIndex >= 0 && searchIndex < pixels.Length && SimilarColors(pixels[searchIndex], ballColor, 2f))
                {
                    counting = true;
                    onBottom += countingDistances[index];
                    break;
                }
            }
        }

        //calculate how many on Right are similar
        counting = true;
        j = 1;
        while (counting)
        {
            counting = false;
            for (int index = 0; index < countingDistances.Length; index++)
            {
                int searchIndex = i + (onRight * j) + (countingDistances[index] * j);
                if (searchIndex >= 0 && searchIndex < pixels.Length && SimilarColors(pixels[searchIndex], ballColor, 2f))
                {
                    counting = true;
                    onRight += countingDistances[index];
                    break;
                }
            }
        }

        //calculate how many on Left are similar
        counting = true;
        j = 1;
        while (counting)
        {
            counting = false;
            for (int index = 0; index < countingDistances.Length; index++)
            {
                int searchIndex = i - (onLeft * j) - (countingDistances[index] * j);
                if (searchIndex >= 0 && searchIndex < pixels.Length && SimilarColors(pixels[searchIndex], ballColor, 2f))
                {
                    counting = true;
                    onLeft += countingDistances[index];
                    break;
                }
            }
        }

        //calculate center of the ball
        centerPos.x = (int)(((i % webcam.width) - onLeft + (i % webcam.width) + onRight) / 2);
        centerPos.y = (int)(((i / webcam.width) + onTop + (i / webcam.width) - onBottom) / 2);
        lastCenterPixel = centerPos.y * webcam.width + centerPos.x;
        int radius = (int)(centerPos.x - ((i % webcam.width) - onLeft));
        return new BallData(centerPos, (int)(Mathf.PI * radius * radius));
    }

    public Vector3 pixelToWorldPosition(Vector2 screenPos)
    {
        Vector3 worldPos = new Vector3(0, 0, -3.5f);
        float xPercent = (float)screenPos.x / webcam.width;
        float yPercent = (float)screenPos.y / webcam.height;

        worldPos.y = (yScale * yPercent) - (yScale/2);
        worldPos.x = (xScale * xPercent) - (xScale/2);
        return worldPos;
    }

    //return true if similar
    public bool SimilarColors(Color32 color1, Color32 color2, float sensitivity = 1)
    {
        return (Mathf.Abs(color1.r - color2.r) + Mathf.Abs(color1.g - color2.g) + Mathf.Abs(color1.b - color2.b)) / 3f < (colorSensitivity * sensitivity);
    }

    void GenerateCubeWall()
    {
        for(int x = 0; x < xScale; x++)
        {
            for(int y = 0; y < yScale; y++)
            {
                Instantiate(cube, new Vector3(x - (xScale / 2) + .5f, y - (yScale / 2) + .5f, -1f), Quaternion.identity, GameObject.Find("Cube Wall").transform);
            }
        }

        foreach(Rigidbody rb in GameObject.Find("Cube Wall").GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
        }
    }


    class BallData
    {
        public Vector2 centerPos;
        public int area;

        public BallData(Vector2 centerPos, int area)
        {
            this.centerPos = centerPos;
            this.area = area;
        }
    }
}
