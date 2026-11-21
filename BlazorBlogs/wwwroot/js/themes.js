(() => {
    document.addEventListener("submit", async event => {
        const form = event.target.closest?.("form.theme-api-form");
        if (!form) return;

        event.preventDefault();
        const button = form.querySelector("button[type='submit']");
        const message = form.closest(".settings-card")?.querySelector(".theme-api-message");
        if (button) button.disabled = true;

        try {
            const response = await fetch(form.action, {
                method: form.dataset.apiMethod || form.method,
                body: new FormData(form),
                credentials: "same-origin"
            });
            if (response.ok) {
                window.location.reload();
                return;
            }

            const contentType = response.headers.get("content-type") || "";
            const payload = contentType.includes("json") ? await response.json() : null;
            const fallback = response.status === 413
                ? "The theme archive exceeds the 10 MiB limit."
                : `Theme operation failed (${response.status}).`;
            if (message) {
                message.textContent = payload?.error || fallback;
                message.classList.add("bb-alert", "bb-alert-danger");
            }
        } catch {
            if (message) {
                message.textContent = "The theme operation could not be completed.";
                message.classList.add("bb-alert", "bb-alert-danger");
            }
        } finally {
            if (button) button.disabled = false;
        }
    });
})();
