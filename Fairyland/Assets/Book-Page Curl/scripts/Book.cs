﻿//The implementation is based on this article:http://rbarraza.com/html5-canvas-pageflip/
//As the rbarraza.com website is not live anymore you can get an archived version from web archive 
//or check an archived version that I uploaded on my website: https://dandarawy.com/html5-canvas-pageflip/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.IO;
using static UnityEngine.Rendering.DebugUI;

public enum FlipMode
{
    RightToLeft,
    LeftToRight
}
[ExecuteInEditMode]
public class Book : MonoBehaviour {
    public Canvas canvas;
    [SerializeField]
    RectTransform BookPanel;
    public Sprite background;
    public Sprite[] bookPages;
    public Sprite[] bookBigPages;
    public bool interactable=true;
    public bool enableShadowEffect=true;
    //represent the index of the sprite shown in the right page
    public int currentPage = 0;

    public int TotalPageCount
    {
        get { return bookPages.Length; }
    }
    public Vector3 EndBottomLeft
    {
        get { return ebl; }
    }
    public Vector3 EndBottomRight
    {
        get { return ebr; }
    }
    public float Height
    {
        get
        {
            return BookPanel.rect.height ; 
        }
    }
    public Image ClippingPlane;
    public Image NextPageClip;
    public Image Shadow;
    public Image ShadowLTR;
    public Image Left;
    public Image LeftNext;
    public Image Right;
    public Image RightNext;
    public UnityEvent OnFlip;
    float radius1, radius2;
    //Spine Bottom
    Vector3 sb;
    //Spine Top
    Vector3 st;
    //corner of the page
    Vector3 c;
    //Edge Bottom Right
    Vector3 ebr;
    //Edge Bottom Left
    Vector3 ebl;
    //follow point 
    Vector3 f;
    bool pageDragging = false;
    //current flip mode
    FlipMode mode;

    //public Transform[] pagePositions; // 페이지 위치를 정의하는 트랜스폼 배열
    //public TMP_FontAsset fontAsset; // 원하는 글꼴
    //public GameObject[] textGameObjects; // 각 페이지의 텍스트를 담는 GameObject 배열

    public TMP_FontAsset fontAsset; // 원하는 글꼴
    private string[] texts;

    public TextMeshProUGUI textObjectLeft;
    public TextMeshProUGUI textObjectRight;
    public TextMeshProUGUI LineGuessingText;

    public GameObject StoryCanvasLeft;
    public GameObject StoryCanvasRight;
    public GameObject LineButtonCanvas;

    public RectTransform LeftTextbox;
    public RectTransform RightTextbox;
    public RectTransform LineGuessing;
    public RectTransform buttonRectTransform;

    public UnityEngine.UI.Button lineButton;
    private bool firstButtonPress = false;
    private bool pressAllowed = true;
    private bool SpeakStartStopButton = true;

    private int LineGuessingPage = 2;




    void Start()
    {
        if (!canvas) canvas=GetComponentInParent<Canvas>();
        if (!canvas) Debug.LogError("Book should be a child to canvas");

        // Call SplitImage to divide the image and set up pages
        if (bookBigPages != null)
        {
            InitializeBookPages();
        }
        else
        {
            Debug.LogError("fullImageSprite not set");
        }

        LoadTextsToPages();
        UpdateTextVisibility(); // 페이지를 넘길 때마다 텍스트 업데이트

        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        UpdateSprites();
        CalcCurlCriticalPoints();

        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        NextPageClip.rectTransform.sizeDelta = new Vector2(pageWidth, pageHeight + pageHeight * 2);


        ClippingPlane.rectTransform.sizeDelta = new Vector2(pageWidth * 2 + pageHeight, pageHeight + pageHeight * 2);

        //hypotenous (diagonal) page length
        float hyp = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
        float shadowPageHeight = pageWidth / 2 + hyp;

        Shadow.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        Shadow.rectTransform.pivot = new Vector2(1, (pageWidth / 2) / shadowPageHeight);

        ShadowLTR.rectTransform.sizeDelta = new Vector2(pageWidth, shadowPageHeight);
        ShadowLTR.rectTransform.pivot = new Vector2(0, (pageWidth / 2) / shadowPageHeight);

    }

    public void OnPressLineGuessing()
    {
        if (!firstButtonPress)
        {
            if (pressAllowed == true && SpeakStartStopButton == true)
            {
                firstButtonPress = true;
                pressAllowed = false;
                StartCoroutine(EnlargeAndCenterImage());
                //StartCoroutine(InitialSequence());
                pressAllowed = true;
            }

        }
    }

    IEnumerator EnlargeAndCenterImage()
    {
        Vector2 originalSize = LineGuessing.sizeDelta;
        Vector2 enlargedSize = new Vector2((float)(LineGuessing.sizeDelta.x * 1.5), (float)(LineGuessing.sizeDelta.y * 2.0));


        Vector2 originalPosition = LineGuessing.anchoredPosition;
        Vector2 originalButtonPosition = buttonRectTransform.anchoredPosition;
        Vector2 targetPosition = Vector2.zero;

        float originalFontSize = LineGuessingText.fontSize;
        float enlargedFontSize = (float)(originalFontSize * 1.5);

        Vector2 originalTextboxSize = LineGuessingText.rectTransform.sizeDelta;
        Vector2 enlargedTextboxSize = new Vector2((float)(originalTextboxSize.x * 1.5), (float)(originalTextboxSize.y * 3.0));

        Vector2 originalButtonSize = buttonRectTransform.sizeDelta;
        Vector2 enlargedButtonSize = new Vector2((float)(buttonRectTransform.sizeDelta.x * 1.8), (float)(buttonRectTransform.sizeDelta.y * 1.4));

        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            LineGuessing.sizeDelta = Vector2.Lerp(originalSize, enlargedSize, t);
            LineGuessing.anchoredPosition = Vector2.Lerp(originalPosition, targetPosition, t);
            LineGuessingText.fontSize = (int)Mathf.Lerp(originalFontSize, enlargedFontSize, t);
            LineGuessingText.rectTransform.sizeDelta = Vector2.Lerp(originalTextboxSize, enlargedTextboxSize, t);
            buttonRectTransform.anchoredPosition = Vector2.Lerp(originalButtonPosition, targetPosition, t);
            buttonRectTransform.sizeDelta = Vector2.Lerp(originalButtonSize, enlargedButtonSize, t);

            yield return null;
        }

        LineGuessing.sizeDelta = enlargedSize;
        LineGuessing.anchoredPosition = targetPosition;
        LineGuessingText.fontSize = (int)enlargedFontSize;
        buttonRectTransform.anchoredPosition = targetPosition;
        buttonRectTransform.sizeDelta = enlargedButtonSize;
    }


    void UpdateTextVisibility()
    {
        Debug.Log("current Page is : " + currentPage);

        if (currentPage == 0 || currentPage / 2 > texts.Length)
        {
            StoryCanvasLeft.SetActive(false);
            StoryCanvasRight.SetActive(false);
            LineButtonCanvas.SetActive(false);
        }
        else
        {

            if (currentPage / 2 <= texts.Length)
            {
                if ((currentPage / 2) % 2 == 0)
                {
                    StoryCanvasLeft.SetActive(true);
                    StoryCanvasRight.SetActive(false);

                    textObjectLeft.text = texts[currentPage / 2 - 1];
                    textObjectLeft.font = fontAsset;
                    textObjectLeft.alignment = currentPage % 2 == 0 ? TextAlignmentOptions.Center : TextAlignmentOptions.Center;

                    if (currentPage / 2 == LineGuessingPage)
                    {
                        LineButtonCanvas.SetActive(true);
                        LineGuessing = LeftTextbox;
                        LineGuessingText = textObjectLeft;
                    }
                    else
                    {
                        LineButtonCanvas.SetActive(false);
                    }
                }
                else
                {
                    StoryCanvasLeft.SetActive(false);
                    StoryCanvasRight.SetActive(true);

                    textObjectRight.text = texts[currentPage / 2 - 1];
                    textObjectRight.font = fontAsset;
                    textObjectRight.alignment = currentPage % 2 == 0 ? TextAlignmentOptions.Center : TextAlignmentOptions.Center;

                    if (currentPage / 2 == LineGuessingPage)
                    {
                        LineButtonCanvas.SetActive(true);
                        LineGuessing = RightTextbox;
                        LineGuessingText = textObjectRight;
                    }
                    else
                    {
                        LineButtonCanvas.SetActive(false);
                    }
                }

            }
            else
            {
                textObjectLeft.text = "";
                textObjectRight.text = "";
            }
        }

        
    }


    void LoadTextsToPages()
    {
        string[] filePaths = Directory.GetFiles(Path.Combine(Application.dataPath, "ResourceTexts"), "*.txt");
        Debug.Log("Total files found: " + filePaths.Length);

        int fileLength = filePaths.Length;

        texts = new string[fileLength];

        for (int pageIndex = 0; pageIndex < fileLength; pageIndex++)
        {
            Debug.Log("Loading text from: " + filePaths[pageIndex]);

            string textContent = File.ReadAllText(filePaths[pageIndex]);
            texts[pageIndex] = textContent;
        }

        Debug.Log("texts Length : " + texts.Length);
    }


    void InitializeBookPages()
    {
        // Assume each big page contains exactly two pages
        bookPages = new Sprite[(bookBigPages.Length + 1) * 2];

        for (int i = 0, j = 0; i < bookBigPages.Length; i++, j += 2)
        {
            Texture2D originalTexture = bookBigPages[i].texture;
            Rect originalRect = bookBigPages[i].rect;

            // Left page
            Rect leftRect = new Rect(originalRect.x, originalRect.y, originalRect.width / 2, originalRect.height);
            bookPages[j + 1] = Sprite.Create(originalTexture, leftRect, new Vector2(0.5f, 0.5f), bookBigPages[i].pixelsPerUnit);

            // Right page
            Rect rightRect = new Rect(originalRect.x + originalRect.width / 2, originalRect.y, originalRect.width / 2, originalRect.height);
            bookPages[j + 2] = Sprite.Create(originalTexture, rightRect, new Vector2(0.5f, 0.5f), bookBigPages[i].pixelsPerUnit);
        }
    }

    void SplitSprite(Sprite originalSprite)
    {
        Texture2D originalTexture = originalSprite.texture;
        Rect originalRect = originalSprite.rect;

        // Calculate left and right rect
        Rect leftRect = new Rect(originalRect.x, originalRect.y, originalRect.width / 2, originalRect.height);
        Rect rightRect = new Rect(originalRect.x + originalRect.width / 2, originalRect.y, originalRect.width / 2, originalRect.height);

        // Create new sprites
        Sprite leftSprite = Sprite.Create(originalTexture, leftRect, new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);
        Sprite rightSprite = Sprite.Create(originalTexture, rightRect, new Vector2(0.5f, 0.5f), originalSprite.pixelsPerUnit);

        bookPages[1] = leftSprite;
        bookPages[2] = rightSprite;

        // Assign sprites to images
        //Left.sprite = leftSprite;
        //Right.sprite = rightSprite;
        
    }

    private void CalcCurlCriticalPoints()
    {
        sb = new Vector3(0, -BookPanel.rect.height / 2);
        ebr = new Vector3(BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        ebl = new Vector3(-BookPanel.rect.width / 2, -BookPanel.rect.height / 2);
        st = new Vector3(0, BookPanel.rect.height / 2);
        radius1 = Vector2.Distance(sb, ebr);
        float pageWidth = BookPanel.rect.width / 2.0f;
        float pageHeight = BookPanel.rect.height;
        radius2 = Mathf.Sqrt(pageWidth * pageWidth + pageHeight * pageHeight);
    }

    public Vector3 transformPoint(Vector3 mouseScreenPos)
    {
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector3 mouseWorldPos = canvas.worldCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, canvas.planeDistance));
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseWorldPos);

            return localPos;
        }
        else if (canvas.renderMode == RenderMode.WorldSpace)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 globalEBR = transform.TransformPoint(ebr);
            Vector3 globalEBL = transform.TransformPoint(ebl);
            Vector3 globalSt = transform.TransformPoint(st);
            Plane p = new Plane(globalEBR, globalEBL, globalSt);
            float distance;
            p.Raycast(ray, out distance);
            Vector2 localPos = BookPanel.InverseTransformPoint(ray.GetPoint(distance));
            return localPos;
        }
        else
        {
            //Screen Space Overlay
            Vector2 localPos = BookPanel.InverseTransformPoint(mouseScreenPos);
            return localPos;
        }
    }
    void Update()
    {
        if (pageDragging && interactable)
        {
            UpdateBook();
        }
    }
    public void UpdateBook()
    {
        f = Vector3.Lerp(f, transformPoint(Input.mousePosition), Time.deltaTime * 10);
        if (mode == FlipMode.RightToLeft)
            UpdateBookRTLToPoint(f);
        else
            UpdateBookLTRToPoint(f);
    }
    public void UpdateBookLTRToPoint(Vector3 followLocation)
    {
        mode = FlipMode.LeftToRight;
        f = followLocation;
        ShadowLTR.transform.SetParent(ClippingPlane.transform, true);
        ShadowLTR.transform.localPosition = new Vector3(0, 0, 0);
        ShadowLTR.transform.localEulerAngles = new Vector3(0, 0, 0);
        Left.transform.SetParent(ClippingPlane.transform, true);

        Right.transform.SetParent(BookPanel.transform, true);
        Right.transform.localEulerAngles = Vector3.zero;
        LeftNext.transform.SetParent(BookPanel.transform, true);

        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebl, out t1);
        //0 < T0_T1_Angle < 180
        clipAngle = (clipAngle + 180) % 180;

        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Left.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Left.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - 90 - clipAngle);

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle - 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        LeftNext.transform.SetParent(NextPageClip.transform, true);
        Right.transform.SetParent(ClippingPlane.transform, true);
        Right.transform.SetAsFirstSibling();

        ShadowLTR.rectTransform.SetParent(Left.rectTransform, true);
    }
    public void UpdateBookRTLToPoint(Vector3 followLocation)
    {
        mode = FlipMode.RightToLeft;
        f = followLocation;
        Shadow.transform.SetParent(ClippingPlane.transform, true);
        Shadow.transform.localPosition = Vector3.zero;
        Shadow.transform.localEulerAngles = Vector3.zero;
        Right.transform.SetParent(ClippingPlane.transform, true);

        Left.transform.SetParent(BookPanel.transform, true);
        Left.transform.localEulerAngles = Vector3.zero;
        RightNext.transform.SetParent(BookPanel.transform, true);
        c = Calc_C_Position(followLocation);
        Vector3 t1;
        float clipAngle = CalcClipAngle(c, ebr, out t1);
        if (clipAngle > -90) clipAngle += 180;

        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);
        ClippingPlane.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        ClippingPlane.transform.position = BookPanel.TransformPoint(t1);

        //page position and angle
        Right.transform.position = BookPanel.TransformPoint(c);
        float C_T1_dy = t1.y - c.y;
        float C_T1_dx = t1.x - c.x;
        float C_T1_Angle = Mathf.Atan2(C_T1_dy, C_T1_dx) * Mathf.Rad2Deg;
        Right.transform.localEulerAngles = new Vector3(0, 0, C_T1_Angle - (clipAngle + 90));

        NextPageClip.transform.localEulerAngles = new Vector3(0, 0, clipAngle + 90);
        NextPageClip.transform.position = BookPanel.TransformPoint(t1);
        RightNext.transform.SetParent(NextPageClip.transform, true);
        Left.transform.SetParent(ClippingPlane.transform, true);
        Left.transform.SetAsFirstSibling();

        Shadow.rectTransform.SetParent(Right.rectTransform, true);
    }
    private float CalcClipAngle(Vector3 c,Vector3 bookCorner,out  Vector3 t1)
    {
        Vector3 t0 = (c + bookCorner) / 2;
        float T0_CORNER_dy = bookCorner.y - t0.y;
        float T0_CORNER_dx = bookCorner.x - t0.x;
        float T0_CORNER_Angle = Mathf.Atan2(T0_CORNER_dy, T0_CORNER_dx);
        float T0_T1_Angle = 90 - T0_CORNER_Angle;
        
        float T1_X = t0.x - T0_CORNER_dy * Mathf.Tan(T0_CORNER_Angle);
        T1_X = normalizeT1X(T1_X, bookCorner, sb);
        t1 = new Vector3(T1_X, sb.y, 0);
        
        //clipping plane angle=T0_T1_Angle
        float T0_T1_dy = t1.y - t0.y;
        float T0_T1_dx = t1.x - t0.x;
        T0_T1_Angle = Mathf.Atan2(T0_T1_dy, T0_T1_dx) * Mathf.Rad2Deg;
        return T0_T1_Angle;
    }
    private float normalizeT1X(float t1,Vector3 corner,Vector3 sb)
    {
        if (t1 > sb.x && sb.x > corner.x)
            return sb.x;
        if (t1 < sb.x && sb.x < corner.x)
            return sb.x;
        return t1;
    }
    private Vector3 Calc_C_Position(Vector3 followLocation)
    {
        Vector3 c;
        f = followLocation;
        float F_SB_dy = f.y - sb.y;
        float F_SB_dx = f.x - sb.x;
        float F_SB_Angle = Mathf.Atan2(F_SB_dy, F_SB_dx);
        Vector3 r1 = new Vector3(radius1 * Mathf.Cos(F_SB_Angle),radius1 * Mathf.Sin(F_SB_Angle), 0) + sb;

        float F_SB_distance = Vector2.Distance(f, sb);
        if (F_SB_distance < radius1)
            c = f;
        else
            c = r1;
        float F_ST_dy = c.y - st.y;
        float F_ST_dx = c.x - st.x;
        float F_ST_Angle = Mathf.Atan2(F_ST_dy, F_ST_dx);
        Vector3 r2 = new Vector3(radius2 * Mathf.Cos(F_ST_Angle),
           radius2 * Mathf.Sin(F_ST_Angle), 0) + st;
        float C_ST_distance = Vector2.Distance(c, st);
        if (C_ST_distance > radius2)
            c = r2;
        return c;
    }
    public void DragRightPageToPoint(Vector3 point)
    {
        if (currentPage >= bookPages.Length) return;
        pageDragging = true;
        mode = FlipMode.RightToLeft;
        f = point;


        NextPageClip.rectTransform.pivot = new Vector2(0, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(1, 0.35f);

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(0, 0);
        Left.transform.position = RightNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage < bookPages.Length) ? bookPages[currentPage] : background;
        Left.transform.SetAsFirstSibling();
        
        Right.gameObject.SetActive(true);
        Right.transform.position = RightNext.transform.position;
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.sprite = (currentPage < bookPages.Length - 1) ? bookPages[currentPage + 1] : background;

        RightNext.sprite = (currentPage < bookPages.Length - 2) ? bookPages[currentPage + 2] : background;

        LeftNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) Shadow.gameObject.SetActive(true);
        UpdateBookRTLToPoint(f);
    }
    public void OnMouseDragRightPage()
    {
        if (interactable)
        DragRightPageToPoint(transformPoint(Input.mousePosition));
        
    }
    public void DragLeftPageToPoint(Vector3 point)
    {
        if (currentPage <= 0) return;
        pageDragging = true;
        mode = FlipMode.LeftToRight;
        f = point;

        NextPageClip.rectTransform.pivot = new Vector2(1, 0.12f);
        ClippingPlane.rectTransform.pivot = new Vector2(0, 0.35f);

        Right.gameObject.SetActive(true);
        Right.transform.position = LeftNext.transform.position;
        Right.sprite = bookPages[currentPage - 1];
        Right.transform.eulerAngles = new Vector3(0, 0, 0);
        Right.transform.SetAsFirstSibling();

        Left.gameObject.SetActive(true);
        Left.rectTransform.pivot = new Vector2(1, 0);
        Left.transform.position = LeftNext.transform.position;
        Left.transform.eulerAngles = new Vector3(0, 0, 0);
        Left.sprite = (currentPage >= 2) ? bookPages[currentPage - 2] : background;

        LeftNext.sprite = (currentPage >= 3) ? bookPages[currentPage - 3] : background;

        RightNext.transform.SetAsFirstSibling();
        if (enableShadowEffect) ShadowLTR.gameObject.SetActive(true);
        UpdateBookLTRToPoint(f);
    }
    public void OnMouseDragLeftPage()
    {
        if (interactable)
        DragLeftPageToPoint(transformPoint(Input.mousePosition));
        
    }
    public void OnMouseRelease()
    {
        if (interactable)
            ReleasePage();
    }
    public void ReleasePage()
    {
        if (pageDragging)
        {
            pageDragging = false;
            float distanceToLeft = Vector2.Distance(c, ebl);
            float distanceToRight = Vector2.Distance(c, ebr);
            if (distanceToRight < distanceToLeft && mode == FlipMode.RightToLeft)
                TweenBack();
            else if (distanceToRight > distanceToLeft && mode == FlipMode.LeftToRight)
                TweenBack();
            else
                TweenForward();
        }
    }
    Coroutine currentCoroutine;
    void UpdateSprites()
    {
        LeftNext.sprite= (currentPage > 0 && currentPage <= bookPages.Length) ? bookPages[currentPage-1] : background;
        RightNext.sprite=(currentPage>=0 &&currentPage<bookPages.Length) ? bookPages[currentPage] : background;
    }
    public void TweenForward()
    {
        if(mode== FlipMode.RightToLeft)
        currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f, () => { Flip(); }));
        else
        currentCoroutine = StartCoroutine(TweenTo(ebr, 0.15f, () => { Flip(); }));
    }
    void Flip()
    {
        if (mode == FlipMode.RightToLeft)
            currentPage += 2;
        else
            currentPage -= 2;
        //currentPage = Mathf.Clamp(currentPage, 0, bookPages.Length - 1); // 페이지 범위 제한
        
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.transform.SetParent(BookPanel.transform, true);
        LeftNext.transform.SetParent(BookPanel.transform, true);
        Left.gameObject.SetActive(false);
        Right.gameObject.SetActive(false);
        Right.transform.SetParent(BookPanel.transform, true);
        RightNext.transform.SetParent(BookPanel.transform, true);
        UpdateSprites();

        UpdateTextVisibility(); // 페이지를 넘길 때마다 텍스트 업데이트

        Shadow.gameObject.SetActive(false);
        ShadowLTR.gameObject.SetActive(false);
        if (OnFlip != null)
            OnFlip.Invoke();
    }
    public void TweenBack()
    {
        if (mode == FlipMode.RightToLeft)
        {
            currentCoroutine = StartCoroutine(TweenTo(ebr,0.15f,
                () =>
                {
                    UpdateSprites();
                    RightNext.transform.SetParent(BookPanel.transform);
                    Right.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
        else
        {
            currentCoroutine = StartCoroutine(TweenTo(ebl, 0.15f,
                () =>
                {
                    UpdateSprites();

                    LeftNext.transform.SetParent(BookPanel.transform);
                    Left.transform.SetParent(BookPanel.transform);

                    Left.gameObject.SetActive(false);
                    Right.gameObject.SetActive(false);
                    pageDragging = false;
                }
                ));
        }
    }
    public IEnumerator TweenTo(Vector3 to, float duration, System.Action onFinish)
    {
        int steps = (int)(duration / 0.025f);
        Vector3 displacement = (to - f) / steps;
        for (int i = 0; i < steps-1; i++)
        {
            if(mode== FlipMode.RightToLeft)
            UpdateBookRTLToPoint( f + displacement);
            else
                UpdateBookLTRToPoint(f + displacement);

            yield return new WaitForSeconds(0.025f);
        }
        if (onFinish != null)
            onFinish();
    }
}
