// Демонстрація обробки REST-запиту у мікросервісі
@RestController
@RequestMapping("/api")
public class RecommendationController {

    @GetMapping("/recommendations")
    public List<String> getRecommendations(@RequestParam String userId) {
        // Повертає список рекомендованих фільмів для користувача
        return List.of("Movie A", "Movie B", "Movie C");
    }
}

// Демонстрація публікації події
public void publishWatchEvent(String userId, String movieId) {
    Event event = new Event("USER_WATCHED_MOVIE", userId, movieId);
    
    // Відправка події у брокер повідомлень
    messageBroker.publish("watch-events", event);
}
