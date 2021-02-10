using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.TerrainAPI;

public class MetaManager : MonoBehaviour
{
    // TO DO move animation controls to another script

    public static MetaManager instance;

    public SetColorInBlock curBlock;
    public int curBlockIndex;
    public Transform blocks;
    [Tooltip("Стоимость покраски одного блока")]
    public float paintingPrice;
    [Tooltip("Время траты одной монеты")]
    public float oneCoinPaintingTime;

    [Tooltip("Кол-во потраченных монет на текущий блок")]
    public int alreadySpent = 0;

    public Transform forestBlocks;
    public CubeAnimation animationCube;
    public float timeBeforeExplosion;

    public CameraSmoothMoving cameraMoving;

    public Animator congratsAnim;

    public bool animating = false;

    public UnityEvent animationStarted;
    public UnityEvent animationFinished;

    private IEnumerator paintingCoroutine;

    void Awake()
    {
        if (!instance)
        {
            instance = this;
            InitializeMeta();
            //instance.curBlockIndex = 0;
            //instance.curBlock = blocks.GetChild(curBlockIndex).GetComponent<SetColorInBlock>();
        }
        else
            Destroy(this.gameObject);
    }

    public void StartPainting() {
        paintingCoroutine = Paint();
        StartCoroutine(paintingCoroutine);
    }

    public void StopPainting()
    {
        StopCoroutine(paintingCoroutine);
    }

    //paint block with spending only 1 coin (balance has to be greater than 0) smoothly for oneCoinPaintingTime
    private void SpendOnePoint()
    {
        SavingSystem.AddToInt(SavingSystem.coins, -1, 0);
        SavingSystem.AddToInt(SavingSystem.spentOnCurBlock, 1, 0);
        alreadySpent++;

        StartCoroutine(curBlock.SmoothAdd(1f / paintingPrice, oneCoinPaintingTime));

        if (alreadySpent == paintingPrice)
            curBlock.intensity = 1f;

        if (curBlock.intensity == 1f)
            BlockPainted();
    }

    private IEnumerator Paint()
    {
        if (animating) yield break;

        //TODO animation if balance == 0

        while(!animating && SavingSystem.GetInt(SavingSystem.coins, 0) != 0)
        {
            SpendOnePoint();
            yield return new WaitForSeconds(oneCoinPaintingTime);
        }
    }

    private void BlockPainted()
    {
        Debug.Log("Block has been painted!!!");

        StartCoroutine(AnimatePaintedBlock());

        alreadySpent = 0;
        SavingSystem.AddToInt(SavingSystem.curBlockIndex, 1, -1);
        SavingSystem.SetInt(SavingSystem.spentOnCurBlock, 0);
    }

    private IEnumerator AnimatePaintedBlock()
    {
        animating = true;
        animationStarted.Invoke();  

        //start congrats animation
        congratsAnim.gameObject.SetActive(true);

        //look at painted block
        yield return cameraMoving.LookAtBlock(curBlock.transform, cameraMoving.cameraFlightSpeed1);

        while (congratsAnim.GetCurrentAnimatorStateInfo(0).IsName("CongratsAnimation"))
            yield return new WaitForEndOfFrame();
        congratsAnim.gameObject.SetActive(false);

        yield return AnimateNextBlock();
    }

    private IEnumerator AnimateNextBlock()
    {
        animating = true;
        animationStarted.Invoke();
        
        curBlockIndex++;

        //look at the next block
        yield return cameraMoving.LookAtBlock(blocks.GetChild(curBlockIndex), cameraMoving.cameraFlightSpeed2);

        animationCube.gameObject.SetActive(true);
        Transform curForestBlock = forestBlocks.GetChild(curBlockIndex);

        //initialize cube's position and size for current forest ground
        animationCube.InitForGround(curForestBlock.GetChild(0));
        //start cube's growing
        yield return animationCube.AnimateGrowing();

        //replace forestBlock with cityBlock
        curForestBlock.gameObject.SetActive(false);
        blocks.GetChild(curBlockIndex).gameObject.SetActive(true);
        curBlock = blocks.GetChild(curBlockIndex).GetComponent<SetColorInBlock>();
        curBlock.intensity = 0f;

        //waiting
        yield return new WaitForSeconds(timeBeforeExplosion);

        //explode the cube
        yield return animationCube.AnimateExplosion();

        animating = false;
        animationFinished.Invoke();
    }

    private void InitializeMeta()
    {
        Debug.Log("Initializing meta...");

        curBlockIndex = SavingSystem.GetInt(SavingSystem.curBlockIndex, -1);

        //DELETE THIS
        //SavingSystem.SetInt(SavingSystem.coins, 1000);


        //setting cityBlocks active and forestBlocks inactive
        for (int i = 0; i <= curBlockIndex; i++)
        {
            forestBlocks.GetChild(i).gameObject.SetActive(false);
            blocks.GetChild(i).gameObject.SetActive(true);
        }

        //start animation
        if (curBlockIndex == -1)
        {
            StartCoroutine(AnimateNextBlock());
            SavingSystem.AddToInt(SavingSystem.curBlockIndex, 1, -1);
        }
        
        curBlock = blocks.GetChild(curBlockIndex).GetComponent<SetColorInBlock>();

        alreadySpent = SavingSystem.GetInt(SavingSystem.spentOnCurBlock, 0);
        curBlock.intensity = (float)alreadySpent / paintingPrice;

        //camera position
        cameraMoving.LookAtBlockInstantly(curBlock.transform);
    }
}
