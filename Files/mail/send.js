let to = document.querySelector("#to");
let from = document.querySelector("#from");
let subject = document.querySelector("#subject");
let text = document.querySelector("#text");

async function Send() {
    if (to.value === "" || from.value === "" || subject.value === "" || text.value === "")
        ShowError("Fill out everything.");
    else {
        let response = await fetch(`send/try?to=${encodeURIComponent(to.value)}&from=${encodeURIComponent(from.value)}&subject=${encodeURIComponent(subject.value)}&text=${encodeURIComponent(text.value)}`, {method:"POST"});
        if (response.status === 200)
            ShowError(await response.text());
        else ShowError("Connection failed.");
    }
}