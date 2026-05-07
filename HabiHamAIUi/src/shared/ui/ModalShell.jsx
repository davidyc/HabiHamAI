import { useEffect } from "react";

function ModalShell({
  open,
  onClose,
  titleId,
  wide = false,
  scroll = false,
  children
}) {
  useEffect(() => {
    if (!open) return undefined;

    const onKeyDown = (event) => {
      if (event.key === "Escape") {
        onClose?.();
      }
    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  const cardClasses = [
    "modal-card",
    wide ? "modal-card--wide" : "",
    scroll ? "modal-card--scroll" : ""
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onClick={(event) => {
        if (event.target === event.currentTarget) {
          onClose?.();
        }
      }}
    >
      <div
        className={cardClasses}
        role="dialog"
        aria-modal="true"
        {...(titleId ? { "aria-labelledby": titleId } : {})}
        onClick={(event) => event.stopPropagation()}
      >
        {children}
      </div>
    </div>
  );
}

export default ModalShell;
