// Add this file and include it in _Host.cshtml (or the app layout).
window.saveAsFile = (filename, bytesBase64) => {
    const link = document.createElement('a');
    link.href = "data:text/csv;charset=utf-8;base64," + bytesBase64;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};