(function () {
  const page = document.body.getAttribute("data-page");
  if (!page) return;

  document.querySelectorAll("[data-nav]").forEach(a => {
    if (a.getAttribute("data-nav") === page) a.classList.add("active");
  });

  const liveCount = document.getElementById("liveCount");
  const ovResp = document.getElementById("ovResp");
  const ovRate = document.getElementById("ovRate");
  if (liveCount && ovResp && ovRate) {
    let count = parseInt(liveCount.textContent || "18", 10);
    setInterval(() => {
      const bump = Math.random() < 0.55 ? 1 : 0;
      count += bump;
      liveCount.textContent = String(count);
      ovResp.textContent = String(126 + (count - 18) * 2);
      ovRate.textContent = String(Math.min(95, 82 + Math.floor((count - 18) * 0.6))) + "%";
    }, 2200);
  }
})();