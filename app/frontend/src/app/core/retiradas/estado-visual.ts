export function classeDoEstado(estado: string): string {
  switch (estado) {
    case 'Solicitada':
      return 'bg-amber-100 text-amber-900';
    case 'Agendada':
      return 'bg-sky-100 text-sky-900';
    case 'Coletada':
      return 'bg-emerald-100 text-emerald-900';
    case 'NaoRealizada':
      return 'bg-orange-100 text-orange-900';
    case 'Recusada':
      return 'bg-red-100 text-red-900';
    case 'Cancelada':
      return 'bg-slate-100 text-slate-700';
    default:
      return 'bg-slate-100 text-slate-700';
  }
}
