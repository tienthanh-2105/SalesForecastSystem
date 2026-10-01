export function validateImage(file: File) {
  return !["image/jpeg", "image/png", "image/webp"].includes(file.type)
    ? "Chỉ nhận JPEG, PNG hoặc WebP."
    : file.size > 5 * 1024 * 1024
      ? "Ảnh tối đa 5 MB."
      : "";
}
