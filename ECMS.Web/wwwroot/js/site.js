(() => {
    const browserTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    if (!browserTimeZone) {
        return;
    }

    const cookieName = "ecms_timezone";
    const encodedTimeZone = encodeURIComponent(browserTimeZone);
    const existingCookie = document.cookie
        .split("; ")
        .find((cookie) => cookie.startsWith(`${cookieName}=`));

    if (existingCookie !== `${cookieName}=${encodedTimeZone}`) {
        document.cookie = `${cookieName}=${encodedTimeZone}; path=/; max-age=31536000; samesite=lax`;
    }

    document.querySelectorAll("[data-browser-timezone]").forEach((element) => {
        if ("value" in element) {
            element.value = browserTimeZone;
        }
    });
})();
