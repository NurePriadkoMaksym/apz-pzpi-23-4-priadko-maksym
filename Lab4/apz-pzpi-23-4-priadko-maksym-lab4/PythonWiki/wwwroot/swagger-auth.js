(async function () {
    const originalFetch = window.fetch;

    window.fetch = async function (url, options) {
        let response = await originalFetch(url, options);

        if (response.status === 401) {
            const refreshToken = localStorage.getItem("refreshToken");

            if (!refreshToken) return response;

            const refreshResponse = await originalFetch("/api/auth/refresh", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    refreshToken: refreshToken
                })
            });

            if (!refreshResponse.ok) return response;

            const data = await refreshResponse.json();

            localStorage.setItem("accessToken", data.accessToken);
            localStorage.setItem("refreshToken", data.refreshToken);

            options.headers["Authorization"] = "Bearer " + data.accessToken;

            return await originalFetch(url, options);
        }

        return response;
    };
})();
