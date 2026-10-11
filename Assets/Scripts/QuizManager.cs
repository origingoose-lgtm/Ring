
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class QuizManager : MonoBehaviour
{
    public enum QuestionType
    {
        MultipleChoice, // Выбрать ответ
        Matching,       // Найти пары
        Ordering        // Расставить по порядку
    }

    [Serializable]
    public class Question
    {
        [Header("Общие настройки")]
        public QuestionType type;

        [TextArea(2, 4)]
        public string questionText;

        [TextArea(2, 4)]
        public string explanation;

        [Header("1. Выбор ответа")]
        public string[] answers;
        public int correctAnswer;

        [Header("2. Сопоставление")]
        public string[] matchingLeft;
        public string[] matchingRight;

        // Для каждого элемента matchingLeft указывается
        // индекс правильной пары в matchingRight.
        public int[] correctMatchForLeft;

        [Header("3. Последовательность")]
        public string[] orderItems;

        // Индексы элементов в правильном порядке.
        public int[] correctOrder;
    }

    [Header("Вопросы")]
    [SerializeField] private List<Question> questions = new();

    [Header("Общий интерфейс")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button nextButton;

    [Header("1. Кнопки выбора ответа")]
    [SerializeField] private Button[] answerButtons;

    [Header("2. Кнопки сопоставления")]
    [SerializeField] private Button[] matchingLeftButtons;
    [SerializeField] private Button[] matchingRightButtons;

    [Header("3. Кнопки последовательности")]
    [SerializeField] private Button[] orderingButtons;

    [Header("События")]
    public UnityEvent onQuizCompleted;
    public UnityEvent onQuizPassed;

    [Header("Настройки результата")]
    [Range(0, 100)]
    [SerializeField] private int passPercent = 70;

    private int currentQuestion;
    private int score;
    private bool answered;
    private bool quizFinished;

    // Состояние сопоставления
    private int selectedLeft = -1;
    private int selectedRight = -1;
    private bool[] matchedLeft;
    private bool[] matchedRight;
    private int matchedCount;

    // Состояние последовательности
    private readonly List<int> selectedOrder = new();
    private int[] displayedOrder;

    private void Start()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextQuestion);
        }

        StartQuiz();
    }

    public void StartQuiz()
    {
        currentQuestion = 0;
        score = 0;
        answered = false;
        quizFinished = false;

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (questions.Count == 0)
        {
            if (questionText != null)
                questionText.text = "Добавьте вопросы в Inspector.";

            HideAllQuestionButtons();
            return;
        }

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentQuestion >= questions.Count)
        {
            FinishQuiz();
            return;
        }

        Question q = questions[currentQuestion];

        answered = false;
        selectedLeft = -1;
        selectedRight = -1;
        matchedCount = 0;
        matchedLeft = null;
        matchedRight = null;
        selectedOrder.Clear();
        displayedOrder = null;

        if (questionText != null)
            questionText.text = q.questionText;

        if (feedbackText != null)
            feedbackText.text = "";

        if (progressText != null)
            progressText.text =
                $"Задание {currentQuestion + 1} из {questions.Count}";

        UpdateScoreText();

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        HideAllQuestionButtons();

        switch (q.type)
        {
            case QuestionType.MultipleChoice:
                SetupMultipleChoice(q);
                break;

            case QuestionType.Matching:
                SetupMatching(q);
                break;

            case QuestionType.Ordering:
                SetupOrdering(q);
                break;
        }
    }

    // -------------------------------------------------
    // ТИП 1: ВЫБОР ОТВЕТА
    // -------------------------------------------------

    private void SetupMultipleChoice(Question q)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            bool available = q.answers != null &&
                             i < q.answers.Length;

            Button button = answerButtons[i];
            button.gameObject.SetActive(available);

            if (!available)
                continue;

            SetButtonLabel(button, q.answers[i]);
            button.interactable = true;
            button.onClick.RemoveAllListeners();

            int answerIndex = i;
            button.onClick.AddListener(
                () => SelectAnswer(answerIndex)
            );
        }
    }

    private void SelectAnswer(int answerIndex)
    {
        if (answered || quizFinished)
            return;

        Question q = questions[currentQuestion];
        bool correct = answerIndex == q.correctAnswer;

        answered = true;

        if (correct)
            score++;

        ShowFeedback(correct, q.explanation);
        UpdateScoreText();

        SetButtonsInteractable(answerButtons, false);
        ShowNextButton();
    }

    // -------------------------------------------------
    // ТИП 2: СОПОСТАВЛЕНИЕ
    // -------------------------------------------------

    private void SetupMatching(Question q)
    {
        int leftCount = q.matchingLeft == null
            ? 0 : q.matchingLeft.Length;

        int rightCount = q.matchingRight == null
            ? 0 : q.matchingRight.Length;

        matchedLeft = new bool[leftCount];
        matchedRight = new bool[rightCount];

        for (int i = 0; i < matchingLeftButtons.Length; i++)
        {
            bool available = i < leftCount;
            Button button = matchingLeftButtons[i];

            button.gameObject.SetActive(available);

            if (!available)
                continue;

            SetButtonLabel(button, q.matchingLeft[i]);
            button.interactable = true;
            button.onClick.RemoveAllListeners();

            int index = i;
            button.onClick.AddListener(
                () => SelectMatchingLeft(index)
            );
        }

        // Перемешиваем правую колонку, чтобы пары
        // не совпадали автоматически по позиции.
        List<int> indices = new();

        for (int i = 0; i < rightCount; i++)
            indices.Add(i);

        Shuffle(indices);

        int visibleCount = Mathf.Min(
            matchingRightButtons.Length, rightCount
        );

        // Сохраняем соответствие между кнопкой и
        // исходным индексом варианта справа.
        rightButtonToOriginalIndex = new int[visibleCount];

        for (int i = 0; i < matchingRightButtons.Length; i++)
        {
            bool available = i < visibleCount;
            Button button = matchingRightButtons[i];

            button.gameObject.SetActive(available);

            if (!available)
                continue;

            int originalIndex = indices[i];
            rightButtonToOriginalIndex[i] = originalIndex;

            SetButtonLabel(
                button, q.matchingRight[originalIndex]
            );

            button.interactable = true;
            button.onClick.RemoveAllListeners();

            int buttonIndex = i;
            button.onClick.AddListener(
                () => SelectMatchingRight(buttonIndex)
            );
        }

        if (leftCount == 0 || rightCount == 0 ||
            q.correctMatchForLeft == null ||
            q.correctMatchForLeft.Length != leftCount ||
            visibleCount != rightCount)
        {
            Debug.LogWarning(
                "Проверьте настройки задания Matching в Inspector."
            );
        }
    }

    private int[] rightButtonToOriginalIndex;

    private void SelectMatchingLeft(int index)
    {
        if (answered || matchedLeft[index])
            return;

        selectedLeft = index;
        TryMatchPair();
    }

    private void SelectMatchingRight(int buttonIndex)
    {
        if (answered)
            return;

        int originalIndex =
            rightButtonToOriginalIndex[buttonIndex];

        if (matchedRight[originalIndex])
            return;

        selectedRight = originalIndex;
        TryMatchPair();
    }

    private void TryMatchPair()
    {
        if (selectedLeft < 0 || selectedRight < 0)
            return;

        Question q = questions[currentQuestion];

        int correctRight =
            q.correctMatchForLeft[selectedLeft];

        if (correctRight == selectedRight)
        {
            matchedLeft[selectedLeft] = true;
            matchedRight[selectedRight] = true;
            matchedCount++;

            matchingLeftButtons[selectedLeft].interactable = false;

            for (int i = 0; i < rightButtonToOriginalIndex.Length; i++)
            {
                if (rightButtonToOriginalIndex[i] == selectedRight)
                    matchingRightButtons[i].interactable = false;
            }

            if (feedbackText != null)
                feedbackText.text = "Пара найдена!";

            if (matchedCount == matchedLeft.Length)
            {
                answered = true;
                score++;

                ShowFeedback(true, q.explanation);
                UpdateScoreText();
                ShowNextButton();
            }
        }
        else
        {
            if (feedbackText != null)
                feedbackText.text =
                    "Это не пара. Попробуйте ещё раз.";
        }

        selectedLeft = -1;
        selectedRight = -1;
    }

    // -------------------------------------------------
    // ТИП 3: ПОСЛЕДОВАТЕЛЬНОСТЬ
    // -------------------------------------------------

    private void SetupOrdering(Question q)
    {
        if (q.orderItems == null || q.correctOrder == null ||
            q.orderItems.Length != q.correctOrder.Length)
        {
            Debug.LogWarning(
                "Проверьте настройки задания Ordering в Inspector."
            );
            return;
        }

        List<int> indices = new();

        for (int i = 0; i < q.orderItems.Length; i++)
            indices.Add(i);

        Shuffle(indices);
        displayedOrder = indices.ToArray();

        int visibleCount = Mathf.Min(
            orderingButtons.Length, q.orderItems.Length
        );

        for (int i = 0; i < orderingButtons.Length; i++)
        {
            bool available = i < visibleCount;
            Button button = orderingButtons[i];

            button.gameObject.SetActive(available);

            if (!available)
                continue;

            int itemIndex = displayedOrder[i];

            SetButtonLabel(
                button,
                $"{q.orderItems[itemIndex]}"
            );

            button.interactable = true;
            button.onClick.RemoveAllListeners();

            int capturedIndex = itemIndex;
            button.onClick.AddListener(
                () => SelectOrderItem(capturedIndex)
            );
        }

        if (visibleCount != q.orderItems.Length)
        {
            Debug.LogWarning(
                "Для этого задания нужно больше orderingButtons."
            );
        }

        if (feedbackText != null)
            feedbackText.text =
                "Нажимайте на карточки в правильном порядке.";
    }

    private void SelectOrderItem(int itemIndex)
    {
        if (answered || selectedOrder.Contains(itemIndex))
            return;

        selectedOrder.Add(itemIndex);

        // Обновляем подписи кнопок: показываем номер
        // выбора перед текстом карточки.
        for (int i = 0; i < orderingButtons.Length; i++)
        {
            if (!orderingButtons[i].gameObject.activeSelf)
                continue;

            int displayedItem = displayedOrder[i];
            int orderPosition = selectedOrder.IndexOf(displayedItem);

            if (orderPosition >= 0)
            {
                SetButtonLabel(
                    orderingButtons[i],
                    $"{orderPosition + 1}. " +
                    questions[currentQuestion].orderItems[displayedItem]
                );

                orderingButtons[i].interactable = false;
            }
        }

        Question q = questions[currentQuestion];

        if (selectedOrder.Count < q.correctOrder.Length)
        {
            if (feedbackText != null)
                feedbackText.text =
                    $"Выбрано: {selectedOrder.Count} из " +
                    $"{q.correctOrder.Length}";
            return;
        }

        bool correct = true;

        for (int i = 0; i < q.correctOrder.Length; i++)
        {
            if (selectedOrder[i] != q.correctOrder[i])
            {
                correct = false;
                break;
            }
        }

        answered = true;

        if (correct)
            score++;

        ShowFeedback(correct, q.explanation);
        UpdateScoreText();

        SetButtonsInteractable(orderingButtons, false);
        ShowNextButton();
    }

    // -------------------------------------------------
    // ОБЩИЕ ФУНКЦИИ
    // -------------------------------------------------

    private void NextQuestion()
    {
        if (!answered || quizFinished)
            return;

        currentQuestion++;

        if (currentQuestion >= questions.Count)
            FinishQuiz();
        else
            ShowQuestion();
    }

    private void FinishQuiz()
    {
        quizFinished = true;
        HideAllQuestionButtons();

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        int percent = questions.Count > 0
            ? Mathf.RoundToInt(score * 100f / questions.Count)
            : 0;

        bool passed = percent >= passPercent;

        if (questionText != null)
            questionText.text = "Викторина завершена!";

        if (feedbackText != null)
        {
            feedbackText.text = passed
                ? "Отличная работа! Вы успешно прошли викторину."
                : "Попробуйте ещё раз, чтобы закрепить знания.";
        }

        if (progressText != null)
            progressText.text = "Готово";

        UpdateScoreText();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text =
                $"Результат: {score} из {questions.Count}\n" +
                $"{percent}%";
        }

        onQuizCompleted?.Invoke();

        if (passed)
            onQuizPassed?.Invoke();
    }

    private void ShowFeedback(bool correct, string explanation)
    {
        if (feedbackText == null)
            return;

        string prefix = correct ? "Верно! " : "Неверно. ";
        feedbackText.text = prefix + explanation;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = $"Баллы: {score}";
    }

    private void ShowNextButton()
    {
        if (nextButton == null)
            return;

        nextButton.gameObject.SetActive(true);

        TMP_Text label =
            nextButton.GetComponentInChildren<TMP_Text>();

        if (label != null)
        {
            label.text = currentQuestion == questions.Count - 1
                ? "Завершить"
                : "Следующее";
        }
    }

    private void HideAllQuestionButtons()
    {
        SetButtonsActive(answerButtons, false);
        SetButtonsActive(matchingLeftButtons, false);
        SetButtonsActive(matchingRightButtons, false);
        SetButtonsActive(orderingButtons, false);
    }

    private void SetButtonsActive(Button[] buttons, bool active)
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.gameObject.SetActive(active);
        }
    }

    private void SetButtonsInteractable(
        Button[] buttons, bool interactable)
    {
        foreach (Button button in buttons)
        {
            if (button != null && button.gameObject.activeSelf)
                button.interactable = interactable;
        }
    }

    private void SetButtonLabel(Button button, string value)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();

        if (label != null)
            label.text = value;
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}