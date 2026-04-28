// Демонстрація клієнтського запиту до API
fetch('/api/recommendations?userId=123')
  .then(response => response.json())
  .then(data => {
    // Відображення списку фільмів
    data.forEach(movie => {
      console.log(movie);
    });
  });
