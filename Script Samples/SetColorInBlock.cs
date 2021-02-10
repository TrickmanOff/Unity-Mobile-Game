using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetColorInBlock : MonoBehaviour
{
    [Range(0f, 1f)] [Tooltip("Показатель бесцветности объекта")]
    public float intensity = 1f;
    public List<int> paintingOrder;
    [Tooltip("Случайный порядок покраски объектов в блоке")]
    public bool randomOrder = false;
    [Tooltip("Seed для генерации порядка покраски объектов в блоке")]
    public int randomSeed = 1;

    private int childrenCount;
    private float prevIntensity = 1f;
    private float childIntensityStep;
    
    private void Start()
    {
        childrenCount = this.transform.childCount;
        childIntensityStep = 1f / childrenCount;

        for (int i = 0; i < childrenCount; i++)
            paintingOrder.Add(i);
        if(randomOrder)
            SetRandomOrder();

        ApplyIntensity();
    }

    private void SetRandomOrder()
    {
        Random.InitState(randomSeed);
        for(int i = childrenCount-1; i >= 0; i--)
        {
            int t = paintingOrder[i];
            int randIndex = Random.Range(0, i + 1);
            paintingOrder[i] = paintingOrder[randIndex];
            paintingOrder[randIndex] = t;
        }
    }

    private void Update()
    {
        if (prevIntensity != intensity)
            ApplyIntensity();
    }

    // плавно добавляет к intensity add за время time
    public IEnumerator SmoothAdd(float add, float time)
    {
        float timer = 0;
        while (timer < time)
        {
            float curAdd = Mathf.Min(1f - intensity, add * (Time.deltaTime / time));
            timer += Time.deltaTime;

            intensity += curAdd;
            yield return new WaitForEndOfFrame();
        }
    }

    // присваивает всем детям Intensity = value (для синхронной раскраски)
    private void SetChildrenIntensityTo(float value)
    {
        foreach(Transform child in this.transform)
        {
            child.GetComponent<MeshRenderer>().material.SetFloat("_Intensity", value);
        }
    }
    
    // применяет текущее значение Intensity
    private void ApplyIntensity()
    {
        //Debug.Log("Intensity " + intensity + " was applied. PrevIntensity = " + prevIntensity);
        int childIndex = Mathf.Min((int) (prevIntensity / childIntensityStep), childrenCount - 1);

        float left = Mathf.Abs(intensity - prevIntensity) * childrenCount;

        while (childIndex >= 0 && childIndex < childrenCount) {
            float elementIntensity = GetElementIntensity(childIndex);
            //Debug.Log(childIndex + "th element's intensity = " + elementIntensity);

            if (intensity < prevIntensity)
            {
                if(left >= elementIntensity)
                {
                    left -= GetElementIntensity(childIndex);
                    SetElementIntensity(childIndex, 0f);

                    childIndex--;
                }
                else
                {
                    SetElementIntensity(childIndex, elementIntensity - left);
                    break;
                }
            }
            else
            {
                if(left + elementIntensity >= 1f)
                {
                    left -= (1f - elementIntensity);
                    SetElementIntensity(childIndex, 1f);

                    childIndex++;
                }
                else
                {
                    SetElementIntensity(childIndex, elementIntensity + left);
                    break;
                }
            }
            //Debug.Log("Left = " + left);
        } 

        prevIntensity = intensity;
    }

    // делает Intensity ребёнка с индексом index равным value
    private void SetElementIntensity(int index, float value)
    {
        index = paintingOrder[index];
        //Debug.Log("Set " + index + "th element's intensity = " + value + " PrevIntensity = " + GetElementIntensity(index));
        //Debug.Log(this.transform.GetChild(index).GetComponent<MeshRenderer>().material.name);
        this.transform.GetChild(index).GetComponent<MeshRenderer>().material.SetFloat("_Intensity", value);
        //Debug.Log("Current Intensity = " + GetElementIntensity(index));
    }

    // возвращает Intensity ребёнка с индексом index
    private float GetElementIntensity(int index)
    {
        index = paintingOrder[index];
        return this.transform.GetChild(index).GetComponent<MeshRenderer>().material.GetFloat("_Intensity");
    }
}
