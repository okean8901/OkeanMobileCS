export function createElement(tag, className, innerHTML = '') {
    const element = document.createElement(tag);
    if (className) element.className = className;
    if (innerHTML) element.innerHTML = innerHTML;
    return element;
}

export function appendToBody(element) {
    document.body.appendChild(element);
}

export function focusElement(element) {
    element.focus();
}

export function scrollToBottom(element) {
    element.scrollTop = element.scrollHeight;
} 